\ Copyright (c) 2024 Travis Bemann
\
\ Permission is hereby granted, free of charge, to any person obtaining a copy
\ of this software and associated documentation files (the "Software"), to deal
\ in the Software without restriction, including without limitation the rights
\ to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
\ copies of the Software, and to permit persons to whom the Software is
\ furnished to do so, subject to the following conditions:
\ 
\ The above copyright notice and this permission notice shall be included in
\ all copies or substantial portions of the Software.
\ 
\ THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
\ IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
\ FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
\ AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
\ LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
\ OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
\ SOFTWARE.

begin-module zscript-armv6m

  \ Out of range 3-bit register exception
  : x-out-of-range-3reg ( -- ) ." out of range 3-bit register" cr ;

  \ Out of range 4-bit register exception
  : x-out-of-range-4reg ( -- ) ." out of range 4-bit register" cr ;

  \ Out of range immediate exception
  : x-out-of-range-imm ( -- ) ." out of range immediate" cr ;

  \ Unaligned immediate exception
  : x-unaligned-imm ( -- ) ." unaligned immediate" cr ;

  \ Out of range PC-relative address exception
  : x-out-of-range-pc-rel ( -- ) ." out of range PC-relative address" cr ;

  \ Incorrect marker type exception
  : x-incorrect-mark-type ( -- ) ." incorrect marker type" cr ;

  \ Invalid condition exception
  : x-invalid-cond ( -- ) ." invalid condition" cr ;

  \ Out of range special register exception
  : x-out-of-range-special ( -- ) ." out of range special register" ;
  
  begin-module zscript-armv6m-internal

    \ Undefer literal
    0 0 foreign forth::undefer-lit undefer-lit
    
    \ Thumb-2?
    0 1 foreign forth::thumb-2? thumb-2?

    \ Get a marker's type
    : mark-type ( marker -- type ) $FFFF and ;

    \ Get a marker's parameter
    : mark-param ( marker -- parameter ) 16 rshift ;

    \ Destination marker type
    0 constant mark-dest

    \ ADR marker type
    1 constant mark-adr

    \ LDR marker type
    2 constant mark-ldr

    \ Conditional branch marker type
    3 constant mark-bc

    \ Unconditional branch marker type
    4 constant mark-b

    \ Validate a 3-bit register
    : validate-3reg ( reg -- ) 8 u< averts x-out-of-range-3reg ;

    \ Validate a 4-bit register
    : validate-4reg ( reg -- ) 16 u< averts x-out-of-range-4reg ;

    \ Validate two 3-bit registers
    : validate-2-3reg ( reg1 reg0 -- ) validate-3reg validate-3reg ;

    \ Validate two 4-bit registers
    : validate-2-4reg ( reg1 reg0 -- ) validate-4reg validate-4reg ;
    
    \ Validate three 3-bit registers
    : validate-3-3reg ( reg2 reg1 reg0 -- )
      validate-3reg validate-3reg validate-3reg
    ;

    \ Validate an immediate as being aligned to a halfword
    : validate-imm-2align ( imm -- ) $1 and 0= averts x-unaligned-imm ;

    \ Validate an immediate as being aligned to a word
    : validate-imm-4align ( imm -- ) $3 and 0= averts x-unaligned-imm ;

    \ Validate a condition
    : validate-cond ( cond -- ) $F u< averts x-invalid-cond ;

    \ Validate a mark
    : validate-mark ( mark mark-type -- )
      swap mark-type = averts x-incorrect-mark-type
    ;

    \ Validate a special register
    : validate-special ( special -- ) 256 u< averts x-out-of-range-special ;
    
    \ Resolve an ADR instruction
    : resolve-adr ( mark-addr mark -- )
      double>2integral
      4 unsafe::align,
      over 2 + 4 align unsafe::here swap -
      dup 1024 u< averts x-out-of-range-pc-rel
      2 rshift swap mark-param 8 lshift or $A000 or swap unsafe::hcurrent!
    ;

    \ Resolve an LDR instruction
    : resolve-ldr ( mark-addr mark -- )
      double>2integral
      4 unsafe::align,
      over 2 + 4 align unsafe::here swap -
      dup 1024 u< averts x-out-of-range-pc-rel
      2 rshift swap mark-param 8 lshift or $4800 or swap unsafe::hcurrent!
    ;

    \ Resolve an unconditional branch
    : resolve-b ( mark -- )
      double>2integral
      drop
      unsafe::here over - 1 arshift
      dup 1024 < over -1025 > and averts x-out-of-range-pc-rel
      $7FF and $E000 or swap 4 - unsafe::hcurrent!
    ;

    \ Resolve a conditional branch
    : resolve-bc ( mark -- )
      double>2integral
      mark-param dup validate-cond { cond }
      unsafe::here over - 1 arshift
      dup 128 < over -129 > and averts x-out-of-range-pc-rel
      $FF and cond 8 lshift or $D000 or swap 4 - unsafe::hcurrent!
    ;

    \ 16-bit constant instruction
    : instr-16-const ( h "name" -- )
      : lit, postpone unsafe::h, postpone ;
    ;

    \ Implement 32-bit constant instruction
    : do-32-const { x -- }
      x dup $FFFF and unsafe::h, 16 rshift unsafe::h,
    ;
    
    \ 32-bit constant instruction
    : instr-32-const ( h0 h1 "name" -- )
      : 16 lshift or lit, postpone do-32-const postpone ;
    ;
    
    \ Implement two three-bit register instruction
    : do-2*3r { r1 r0 op -- }
      r1 r0 validate-2-3reg r1 3 lshift r0 or op or unsafe::h,
    ;
    
    \ 16-bit two three-bit register instruction
    : instr-2*3r ( h "name" -- )
      : lit, postpone do-2*3r postpone ;
    ;

    \ Implement three three-bit register instruction
    : do-3*3r { r2 r1 r0 op -- }
      r2 r1 r0 validate-3-3reg r1 3 lshift r0 or r2 6 lshift or op or unsafe::h,
    ;
    
    \ 16-bit three three-bit register instruction
    : instr-3*3r ( h "name" -- )
      : lit, postpone do-3*3r postpone ;
    ;
    
    \ Implement two three-bit register with imm3 immediate instruction
    : do-2*3r-3imm { imm r1 r0 op -- }
      r1 r0 validate-2-3reg imm 8 u< averts x-out-of-range-imm
      r1 3 lshift r0 or imm 6 lshift or op or unsafe::h,
    ;
    
    \ 16-bit two three-bit register with imm3 immediate instruction
    : instr-2*3r-3imm ( h "name" -- )
      : lit, postpone do-2*3r-3imm postpone ;
    ;

    \ Implement one three-bit register with imm8 immediate instruction
    : do-3r-8imm { imm r0 op -- }
      r0 validate-3reg imm 256 u< averts x-out-of-range-imm
      r0 8 lshift imm or op or unsafe::h,
    ;

    \ 16-bit one three-bit register with imm8 immediate instruction
    : instr-3r-8imm ( h "name"  -- )
      : lit, postpone do-3r-8imm postpone ;
    ;

    \ Implement 16-bit three-bit register load/store 4-byte-immediate
    \ instruction
    : do-3r-load/store-4byte-imm { imm r1 r0 op -- }
      r1 r0 validate-2-3reg imm validate-imm-4align
      r1 3 lshift r0 or imm 2 rshift dup 32 u< averts x-out-of-range-imm
      6 lshift or op or unsafe::h,
    ;

    \ 16-bit three-bit register load/store 4-byte-immediate instruction
    : instr-3r-load/store-4byte-imm ( h "name" -- )
      : lit, postpone do-3r-load/store-4byte-imm postpone ;
    ;

    \ Implement 16-bit three-bit register load/store SP-relative
    \ 4-byte-immediate instruction
    : do-3r-load/store-sp-4byte-imm { imm r0 op -- }
      r0 validate-3reg imm validate-imm-4align
      r0 8 lshift imm 2 rshift dup 256 u< averts x-out-of-range-imm
      or op or unsafe::h,
    ;
    
    \ 16-bit three-bit register load/store SP-relative 4-byte-immediate
    \ instruction
    : instr-3r-load/store-sp-4byte-imm ( h "name" -- )
      : lit, postpone do-3r-load/store-sp-4byte-imm postpone ;
    ;
    
    \ Implement 16-bit three-bit register load/store relative instruction
    : do-3r-load/store-rel { r2 r1 r0 op -- }
      r2 r1 r0 validate-3-3reg r1 3 lshift r0 or r2 6 lshift or op or unsafe::h,
    ;
    
    \ 16-bit three-bit register load/store relative instruction
    : instr-3r-load/store-rel ( h "name" -- )
      : lit, postpone do-3r-load/store-rel postpone ;
    ;

    \ Implement 16-bit three-bit register load/store 1-byte-immediate
    \ instruction
    : do-3r-load/store-1byte-imm { imm r1 r0 op -- }
      r1 r0 validate-2-3reg
      r1 3 lshift r0 or imm dup 32 u< averts x-out-of-range-imm
      6 lshift or op or unsafe::h,
    ;

    \ 16-bit three-bit register load/store 1-byte-immediate instruction
    : instr-3r-load/store-1byte-imm ( h "name" -- )
      : lit, postpone do-3r-load/store-1byte-imm postpone ;
    ;

    \ Implement 16-bit three-bit register load/store 1-byte-immediate
    \ instruction
    : do-3r-load/store-2byte-imm { imm r1 r0 op -- }
      r1 r0 validate-2-3reg imm validate-imm-2align
      r1 3 lshift r0 or imm 1 rshift dup 32 u< averts x-out-of-range-imm
      6 lshift or op or unsafe::h,
    ;

    \ 16-bit three-bit register load/store 2-byte-immediate instruction
    : instr-3r-load/store-2byte-imm ( h "name" -- )
      : lit, postpone do-3r-load/store-2byte-imm postpone ;
    ;

    \ Implement 16-bit three-bit register load/store multiple instruction
    : do-3r-load/store-multi { r0 op -- }
      r0 validate-3reg
      0 begin over while rot dup validate-3reg bit or swap 1- swap repeat nip
      r0 8 lshift or op or unsafe::h,
    ;
    
    \ 16-bit three-bit register load/store multiple instruction
    : instr-3r-load/store-multi ( h "name" -- )
      : lit, postpone do-3r-load/store-multi postpone ;
    ;
    
    \ Implement 16-bit 8imm immediate instruction
    : do-8imm { imm op -- }
      imm 256 u< averts x-out-of-range-imm
      imm op or unsafe::h,
    ;
    
    \ 16-bit 8imm immediate instruction
    : instr-8imm ( h "name" -- )
      : lit, postpone do-8imm postpone ;
    ;
    
    \ Implement 16-bit one four-bit register instruction
    : do-4r { r0 op -- }
      r0 validate-4reg
      r0 3 lshift op or unsafe::h,
    ;
    
    \ 16-bit one four-bit register instruction
    : instr-4r ( h "name" -- )
      : lit, postpone do-4r postpone ;
    ;
    
    \ Implement 16-bit two four-bit register instruction
    : do-2*4r { r1 r0 op -- }
      r1 r0 validate-2-4reg
      r0 dup $7 and swap $8 and 4 lshift or r1 3 lshift or op or unsafe::h,
    ;
    
    \ 16-bit two four-bit register instruction
    : instr-2*4r ( h "name" -- )
      : lit, postpone do-2*4r postpone ;
    ;

  end-module> import

  begin-module zscript-armv6m-instr

    \ Registers
    0 constant r0
    1 constant r1
    2 constant r2
    3 constant r3
    4 constant r4
    5 constant r5
    6 constant r6
    7 constant r7
    8 constant r8
    9 constant r9
    10 constant r10
    11 constant r11
    12 constant r12
    13 constant r13
    14 constant r14
    15 constant r15
    r6 constant tos
    r7 constant dp
    r13 constant sp
    r14 constant lr
    r15 constant pc

    \ Condition codes
    $0 constant eq
    $1 constant ne
    $2 constant cs
    $2 constant hs
    $3 constant cc
    $3 constant lo
    $4 constant mi
    $5 constant pl
    $6 constant vs
    $7 constant vc
    $8 constant hi
    $9 constant ls
    $A constant ge
    $B constant lt
    $C constant gt
    $D constant le
    $E constant al

    \ Assemble an ADCS instruction
    $4140 instr-2*3r adcs_,_ ( rm rdn -- )

    \ Assemble a two-register ADDS immediate instruction
    $1C00 instr-2*3r-3imm adds_,_,#_ ( imm3 rn rd -- )

    \ Assemble a one-register ADDS immediate instruction
    $3000 instr-3r-8imm adds_,#_ ( imm8 rdn -- )

    \ Assemble a three-register ADDS register instruction
    $1800 instr-3*3r adds_,_,_ ( rm rn rd -- )

    \ Assemble a two-4-bit register ADDS register instruction
    $4400 instr-2*4r add4_,4_ ( rm4 rdn4 -- )

    \ Assemble an ADD (SP plus immediate) instruction
    : add_,sp,#_ ( imm8 rd -- )
      dup validate-3reg over validate-imm-4align
      over 1024 u< averts x-out-of-range-imm
      8 lshift swap 2 rshift or $A800 or unsafe::h,
    ;

    \ Assemble an ADD (SP plus immediate to SP) instruction
    : addsp,sp,#_ ( imm7 -- )
      dup validate-imm-4align dup 512 u< averts x-out-of-range-imm
      2 rshift $B000 or unsafe::h,
    ;

    \ Assemble an ADD (SP plus register) instruction
    : add4_,sp ( rdm4 -- )
      dup validate-4reg dup $7 and swap $8 and 4 lshift or $4468 or unsafe::h,
    ;

    \ Assemble an ADD (SP plus register to SP) instruction
    : addsp,sp,4_ ( rm4 -- )
      dup validate-4reg 3 lshift $4485 or unsafe::h,
    ;

    \ Mark an ADR instruction
    : adr_ ( rd -- mark )
      dup validate-3reg unsafe::hreserve swap 16 lshift mark-adr or
      2integral>double
    ;

    \ Assemble an ANDS (register) instruction
    $4000 instr-2*3r ands_,_ ( rm rdn -- )

    \ Assemble an ASRS (immediate) instruction
    : asrs_,_,#_ ( imm5 rm rd -- )
      2dup validate-2-3reg
      2 pick 1 u>= averts x-out-of-range-imm
      2 pick 33 u< averts x-out-of-range-imm
      swap 3 lshift or swap dup 32 = if drop 0 then 6 lshift or $1000 or
      unsafe::h,
    ;

    \ Assemble an ASRS (register) instruction
    $4100 instr-2*3r asrs_,_ ( rm rdn -- )

    \ Assemble an unconditional branch to a marker
    : b< ( mark -- )
      double>2integral
      mark-dest validate-mark
      unsafe::here 4 + - 1 arshift
      dup 1024 < over -1025 > and averts x-out-of-range-pc-rel
      $7FF and $E000 or unsafe::h,
    ;

    \ Assemble a conditional branch to a marker
    : bc< ( mark cond -- )
      swap double>2integral rot
      dup validate-cond
      swap mark-dest validate-mark
      swap unsafe::here 4 + - 1 arshift
      dup 128 < over -129 > and averts x-out-of-range-pc-rel
      $FF and swap 8 lshift or $D000 or unsafe::h,
    ;

    \ Mark an unconditional branch
    : b> ( -- mark )
      unsafe::hreserve 4 + mark-b 2integral>double
    ;

    \ Mark a conditional branch
    : bc> ( cond -- mark )
      dup validate-cond unsafe::hreserve 4 + swap 16 lshift mark-bc or
      2integral>double
    ;

    \ Assemble an BICS (register) instruction
    $4380 instr-2*3r bics_,_ ( rm rdn -- )

    \ Assemble a BKPT instruction
    $BE00 instr-8imm bkpt#_ ( imm8 -- )

    \ We are not implementing BL; it is up to the user to use normal word calls
    \ for this

    \ Assemble a BLX (register) instruction
    $4780 instr-4r blx_ ( rm -- )

    \ Assemble a BX instruction
    $4700 instr-4r bx_ ( rm -- )

    \ Assemble an CMN (register) instruction
    $42C0 instr-2*3r cmn_,_ ( rm rn -- )

    \ Assemble a CMP (immediate) instruction
    $2800 instr-3r-8imm cmp_,#_ ( imm8 rn -- )

    \ Assemble an CMP (register) instruction
    $4280 instr-2*3r cmp_,_ ( rm rn -- )

    \ Assemble an CMP (register) instruction
    $4500 instr-2*4r cmp4_,4_ ( rm4 rdn4 -- )

    \ Assemble a CPSIE instruction
    $B662 instr-16-const cpsie

    \ Assemble a CPSID instruction
    $B672 instr-16-const cpsid ( -- )

    \ Assemble a DMB instruction
    $F3BF $8F5F instr-32-const dmb ( -- )

    \ Assemble a DSB instruction
    $F3BF $8F4F instr-32-const dsb ( -- )

    \ Assemble an EORS (register) instruction
    $4040 instr-2*3r eors_,_ ( rm rdn -- )

    \ Assemble an ISB instruction
    $F3BF $8F6F instr-32-const isb ( -- )

    \ Assemble an LDM instruction
    $C800 instr-3r-load/store-multi ldm ( rx ... r0 count rn -- )

    \ Assemble an LDR (immediate) instruction
    $6800 instr-3r-load/store-4byte-imm ldr_,[_,#_] ( imm5 rn rt -- )

    \ Assemble an LDR (immediate) instruction
    $9800 instr-3r-load/store-sp-4byte-imm ldr_,[sp,#_] ( imm8 rt -- )

    \ Mark an LDR (literal) instruction
    : ldr_,[pc] ( rd -- mark )
      dup validate-3reg unsafe::hreserve swap 16 lshift mark-ldr or
      2integral>double
    ;

    \ Assemble an LDR (register) instruction
    $5800 instr-3r-load/store-rel ldr_,[_,_] ( rm rn rt -- )

    \ Assemble an LDRB (immediate) instruction
    $7800 instr-3r-load/store-1byte-imm ldrb_,[_,#_] ( imm5 rn rt -- )

    \ Assemble an LDRB (register) instruction
    $5C00 instr-3r-load/store-rel ldrb_,[_,_] ( rm rn rt -- )

    \ Assemble an LDRH (immediate) instruction
    $8800 instr-3r-load/store-2byte-imm ldrh_,[_,#_] ( imm5 rn rt -- )

    \ Assemble an LDRH (register) instruction
    $5A00 instr-3r-load/store-rel ldrh_,[_,_] ( rm rn rt -- )

    \ Assemble an LDRSB (register) instruction
    $5600 instr-3r-load/store-rel ldrsb_,[_,_] ( rm rn rt -- )

    \ Assemble an LDRSH (register) instruction
    $5E00 instr-3r-load/store-rel ldrsh_,[_,_] ( rm rn rt -- )

    \ Assemble an LSLS (immediate) instruction
    : lsls_,_,#_ ( imm5 rm rd -- )
      2dup validate-2-3reg
      swap 3 lshift or swap 6 lshift or $0000 or unsafe::h,
    ;

    \ Assemble an LSLS (register) instruction
    $4080 instr-2*3r lsls_,_ ( rm rdn -- )
    
    \ Assemble an LSRS (immediate) instruction
    : lsrs_,_,#_ ( imm5 rm rd -- )
      2dup validate-2-3reg
      2 pick 1 u>= averts x-out-of-range-imm
      2 pick 33 u< averts x-out-of-range-imm
      swap 3 lshift or swap dup 32 = if drop 0 then 6 lshift or $0800 or
      unsafe::h,
    ;

    \ Assemble an LSRS (register) instruction
    $40C0 instr-2*3r lsrs_,_ ( rm rdn -- )

    \ Assemble a MOVS (immediate) instruction
    $2000 instr-3r-8imm movs_,#_ ( imm8 rdn -- )

    \ Assemble a MOV (register) instruction
    $4600 instr-2*4r mov4_,4_ ( rm4 rdn4 -- )

    \ Assemble a MOVS (register) instruction
    $0000 instr-2*3r movs_,_ ( rm rd -- )

    \ Assemble an MRS instruction
    : mrs_,_ ( sysm rd -- )
      dup validate-4reg over validate-special
      $F3EF h, 8 lshift or $8000 or unsafe::h,
    ;

    \ Assemble an MSR (register) instruction
    : msr_,_ ( rn sysm -- )
      dup validate-special over validate-4reg
      swap $F380 or h, $8800 or unsafe::h,
    ;
    
    \ Assemble an MULS (register) instruction
    $4340 instr-2*3r muls_,_ ( rn rdm -- )

    \ Assemble an MVNS (register) instruction
    $43C0 instr-2*3r mvns_,_ ( rm rd -- )

    \ Assemble a NOP instruction
    $BF00 instr-16-const nop ( -- )

    \ Assemble an ORRS (register) instruction
    $4300 instr-2*3r orrs_,_ ( rm rdn -- )

    \ Assemble a POP instruction
    : pop ( rx ... r0 count -- )
      0 begin over while
	rot dup pc = if
	  drop 8 bit or
	else
	  dup validate-3reg bit or
	then
	swap 1- swap
      repeat
      nip $BC00 or unsafe::h,
    ;

    \ Assemble a PUSH instruction
    : push ( rx ... r0 count -- )
      0 begin over while
	rot dup lr = if
	  drop 8 bit or
	else
	  dup validate-3reg bit or
	then
	swap 1- swap
      repeat
      nip $B400 or unsafe::h,
    ;

    \ Assemble an REV (register) instruction
    $BA00 instr-2*3r rev_,_ ( rm rd -- )

    \ Assemble an REV16 (register) instruction
    $BA40 instr-2*3r rev16_,_ ( rm rd -- )

    \ Assemble an REVSH (register) instruction
    $BAC0 instr-2*3r revsh_,_ ( rm rd -- )

    \ Assemble an RORS (register) instruction
    $41C0 instr-2*3r rors_,_ ( rm rdn -- )

    \ Assemble an RSBS (immediate) instruction
    $4240 instr-2*3r rsbs_,_,#0 ( rn rd -- )

    \ Assemble an SBC (register) instruction
    $4180 instr-2*3r sbcs_,_ ( rm rdn -- )

    \ Assemble an SEV instruction
    $BF40 instr-16-const sev ( -- )

    \ Assemble an STM instruction
    $C000 instr-3r-load/store-multi stm ( rx ... r0 count rn -- )

    \ Assemble an STR (immediate) instruction
    $6000 instr-3r-load/store-4byte-imm str_,[_,#_] ( imm5 rn rt -- )

    \ Assemble an STR (immediate) instruction
    $9000 instr-3r-load/store-sp-4byte-imm str_,[sp,#_] ( imm8 rt -- )

    \ Assemble an STR (register) instruction
    $5000 instr-3r-load/store-rel str_,[_,_] ( rm rn rt -- )
    
    \ Assemble an STRB (immediate) instruction
    $7000 instr-3r-load/store-1byte-imm strb_,[_,#_] ( imm5 rn rt -- )

    \ Assemble an STRB (register) instruction
    $5400 instr-3r-load/store-rel strb_,[_,_] ( rm rn rt -- )

    \ Assemble an STRH (immediate) instruction
    $8000 instr-3r-load/store-2byte-imm strh_,[_,#_] ( imm5 rn rt -- )

    \ Assemble an STRH (register) instruction
    $5200 instr-3r-load/store-rel strh_,[_,_] ( rm rn rt -- )

    \ Assemble a two-register SUBS immediate instruction
    $1E00 instr-2*3r-3imm subs_,_,#_ ( imm3 rn rd -- )

    \ Assemble a one-register SUBS immediate instruction
    $3800 instr-3r-8imm subs_,#_ ( imm8 rdn -- )

    \ Assemble a three-register SUBS register instruction
    $1A00 instr-3*3r subs_,_,_ ( rm rn rd -- )

    \ Assemble an SUB (SP plus immediate to SP) instruction
    : subsp,sp,#_ ( imm7 -- )
      dup validate-imm-4align dup 512 u< averts x-out-of-range-imm
      2 rshift $B080 or unsafe::h,
    ;

    \ Assemble an SVC instruction
    $BF00 instr-8imm svc#_ ( imm8 -- )

    \ Assemble an SXTB instruction
    $B240 instr-2*3r sxtb_,_ ( rm rd -- )
    
    \ Assemble an SXTH instruction
    $B200 instr-2*3r sxth_,_ ( rm rd -- )

    \ Assemble an TST (register) instruction
    $4200 instr-2*3r tst_,_ ( rm rn -- )

    \ Assemble a UDF instruction
    $DE00 instr-8imm udf#_ ( imm8 -- )

    \ Assemble a 32-bit UDF instruction
    : udf.w#_ ( imm16 -- )
      dup 65536 u< averts x-out-of-range-imm
      dup 12 rshift $F and $F7F0 or unsafe::h,
      $FFF and $A000 or unsafe::h,
    ;

    \ Assemble an UXTB instruction
    $B2C0 instr-2*3r uxtb_,_ ( rm rd -- )

    \ Assemble an UXTH instruction
    $B280 instr-2*3r uxth_,_ ( rm rd -- )

    \ Assemble a WFE instruction
    $BF20 instr-16-const wfe ( -- )

    \ Assemble a WFI instruction
    $BF30 instr-16-const wfi ( -- )

    \ Assemble a YIELD instruction
    $BF10 instr-16-const yield ( -- )
    
    \ Mark a backward destination a fashion that makes more sense.
    : mark< ( -- mark ) unsafe::here mark-dest 2integral>double ;

    \ Mark a backward destination (deprecated)
    : mark> ( -- mark ) mark< ;

    \ Mark a forward destination
    : >mark ( mark -- )
      dup double>2integral nip mark-type case
	mark-adr of resolve-adr endof
	mark-ldr of resolve-ldr endof
	mark-b of resolve-b endof
	mark-bc of resolve-bc endof
	['] x-incorrect-mark-type ?raise
      endcase
    ;
    
  end-module

  \ Begin an assembly block
  : code[
    [compile-only] [immediate]
    undefer-lit
    [ thumb-2? not ] [if] internal::consts-inline, [then]
    zscript-armv6m-instr import
    postpone [
  ;

  \ End an assembly block
  : ]code zscript-armv6m-instr unimport ] ;
  
end-module
