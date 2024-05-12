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

begin-module zscript-double

  \ The saved handle-number-hook
  global saved-handle-number-hook

  \ The handle-number-hook foreign variable
  foreign-hook-variable forth::handle-number-hook handle-number-hook

  \ Convert a 32-bit integer to a double cell
  : s>d ( x -- dvalue )
    zscript-internal::integral> 0 zscript-internal::>double
  ;

  \ Convert a double cell to a 32-bit integer
  : d>s ( x -- dvalue )
    zscript-internal::double> drop zscript-internal::>integral
  ;

  \ Convert a 32-bit integer to a S31.32 fixed-point number
  : s>f ( x -- fvalue )
    zscript-internal::integral> 0 swap zscript-internal::>double
  ;
  
  \ Test for the quality of two doubles
  : d= ( dvalue0 dvalue1 -- equal? )
    zscript-internal::2double> forth::d= zscript-internal::>integral
  ;

  \ Test for the inequality of two doubles
  : d<> ( dvalue0 dvalue1 -- not-equal? )
    zscript-internal::2double> forth::d<> zscript-internal::>integral
  ;

  \ Unsigned double less than
  : du< ( dvalue0 dvalue1 -- flag? )
    zscript-internal::2double> forth::du< zscript-internal::>integral
  ;

  \ Unsigned double greater than
  : du> ( dvalue0 dvalue1 -- flag? )
    zscript-internal::2double> forth::du> zscript-internal::>integral
  ;

  \ Unsigned double greater than or equal
  : du>= ( dvalue0 dvalue1 -- flag? )
    zscript-internal::2double> forth::du>= zscript-internal::>integral
  ;

  \ Unsigned double less than or equal
  : du<= ( dvalue0 dvalue1 -- flag? )
    zscript-internal::2double> forth::du<= zscript-internal::>integral
  ;

  \ Signed double less than
  : d< ( dvalue0 dvalue1 -- flag? )
    zscript-internal::2double> forth::d< zscript-internal::>integral
  ;

  \ Signed double greater than
  : d> ( dvalue0 dvalue1 -- flag? )
    zscript-internal::2double> forth::d> zscript-internal::>integral
  ;

  \ Signed double greater than or equal than
  : d>= ( dvalue0 dvalue1 -- flag? )
    zscript-internal::2double> forth::d>= zscript-internal::>integral
  ;

  \ Signed double less than or equal than
  : d<= ( dvalue0 dvalue1 -- flag? )
    zscript-internal::2double> forth::d<= zscript-internal::>integral
  ;

  \ Double equals zero
  : d0= ( dvalue -- flag? )
    zscript-internal::double> forth::d0= zscript-internal::>integral
  ;

  \ Double not equals zero
  : d0<> ( dvalue -- flag? )
    zscript-internal::double> forth::d0<> zscript-internal::>integral
  ;

  \ Double less than zero
  : d0< ( dvalue -- flag? )
    zscript-internal::double> forth::d0< zscript-internal::>integral
  ;

  \ Double greater than zero
  : d0> ( dvalue -- flag? )
    zscript-internal::double> forth::d0> zscript-internal::>integral
  ;
	
  \ Double less than or equal to zero
  : d0<= ( dvalue -- flag? )
    zscript-internal::double> forth::d0<= zscript-internal::>integral
  ;

  \ Double greater than or equal to zero
  : d0>= ( dvalue -- flag? )
    zscript-internal::double> forth::d0>= zscript-internal::>integral
  ;

  \ Double left shift
  : 2lshift ( dvalue0 x -- dvalue2 )
    zscript-internal::integral> swap zscript-internal::double> rot
    forth::2lshift zscript-internal::>double
  ;

  \ Double right shift
  : 2rshift ( dvalue0 x -- dvalue2 )
    zscript-internal::integral> swap zscript-internal::double> rot
    forth::2rshift zscript-internal::>double
  ;

  \ Double arithmetic right shift
  : 2arshift ( dvalue0 x -- dvalue2 )
    zscript-internal::integral> swap zscript-internal::double> rot
    forth::2arshift zscript-internal::>double
  ;
	
  \ Negate a double word
  : dnegate ( dvalue0 -- dvalue1 )
    zscript-internal::double> forth::dnegate zscript-internal::>double
  ;

  \ Add two double words
  : d+ ( dvalue0 dvalue1 -- dvalue2 )
    zscript-internal::2double> forth::d+ zscript-internal::>double
  ;

  \ Subtract two double words
  : d- ( dvalue0 dvalue1 -- dvalue2 )
    zscript-internal::2double> forth::d- zscript-internal::>double
  ;

  \ Signed multiply 64 * 64 = 64
  : d* ( ndvalue0 ndvalue1 -- dvalue2 )
    zscript-internal::2double> forth::d* zscript-internal::>double
  ;
  
  \ Unsigned multiply 64 * 64 = 64
  : ud* ( udvalue0 udvalue1 -- dvalue2 )
    zscript-internal::2double> forth::ud* zscript-internal::>double
  ;

  \ Unsigned multiply 64 * 64 = 128
  : udm* ( udvalue0 udvalue1 -- dvaluel dvalueh )
    zscript-internal::2double> forth::udm* zscript-internal::2>double
  ;

  \ Signed 32*32/32
  3 1 foreign forth::*/ */ ( n0 n1 n2 -- n0*n1/n2 )

  \ Signed 32*32/32 with modulus
  3 2 foreign forth::*/mod */mod ( n0 n1 n2 n0*n1/modn2 n0*n1/n2 )

  \ Unsigned 32*32/32
  3 1 foreign forth::u*/ u*/ ( u0 u1 u2 -- u0*u1/u2 )

  \ Unsigned 32*32/32 with modulus
  3 2 foreign forth::u*/mod u*/mod ( u0 u1 u2 -- u0*u1/modu2 u0*u1/u2 )

  \ Signed 64/32 = 32 remainder 32 division
  : m/mod ( ndvalue nvalue -- rem div )
    zscript-internal::integral> -rot zscript-internal::double> rot forth::m/mod
    zscript-internal::2>integral
  ;

  \ Unsigned 64/32 = 32 remainder 32 division
  : um/mod ( udvalue uvalue -- rem div )
    zscript-internal::integral> -rot zscript-internal::double> rot forth::um/mod
    zscript-internal::2>integral
  ;

  \ Signed divide 64/64 = 64 remainder 64 division
  : d/mod ( ndvalue0 ndvalue1 -- drem ddiv )
    zscript-internal::2double> forth::d/mod zscript-internal::2>double
  ;

  \ Unsigned divide 64/64 = 64 remainder 64 division
  : ud/mod ( udvalue0 udvalue1 -- drem ddiv )
    zscript-internal::2double> forth::ud/mod zscript-internal::2>double
  ;

  \ Signed divide 64/64 = 64 division
  : d/ ( ndvalue0 ndvalue1 -- ddiv )
    zscript-internal::2double> forth::d/ zscript-internal::>double
  ;

  \ Unsigned divide 64/64 = 64 division
  : ud/ ( udvalue0 udvalue1 -- ddiv )
    zscript-internal::2double> forth::ud/ zscript-internal::>double
  ;

  \ S31.32 multiplication
  : f* ( fvalue0 fvalue1 -- fvalue2 )
    zscript-internal::2double> forth::f* zscript-internal::>double
  ;

  \ S31.32 division
  : f/ ( fvalue0 fvalue1 -- fvalue2 )
    zscript-internal::2double> forth::f/ zscript-internal::>double
  ;

  \ Get the absolute value of a double-cell number
  : dabs ( ndvalue -- udvalue )
    zscript-internal::double> forth::dabs zscript-internal::>double
  ;
  
  \ Get the minimum of two double-cell numbers
  : dmin ( ndvalue0 ndvalue1 -- dvalue2 )
    zscript-internal::2double> forth::dmin zscript-internal::>double
  ;

  \ Get the maximum of two double-cell numbers
  : dmax ( ndvalue0 ndvalue1 -- dvalue2 )
    zscript-internal::2double> forth::dmax zscript-internal::>double
  ;

  \ Get the value of pi
  foreign-double-constant forth::pi pi

  \ Get the ceiling of a fixed-point number as a single-cell number
  : ceil ( fvalue -- nvalue )
    zscript-internal::double> forth::ceil zscript-internal::>integral
  ;

  \ Get the floor of a fixed-point number as a single-cell number
  : floor ( fvalue -- nvalue )
    zscript-internal::double> forth::floor zscript-internal::>integral
  ;

  \ Round a fixed-point number to the nearest integer with half rounding up
  : round-half-up ( fvalue -- nvalue )
    zscript-internal::double> forth::round-half-up zscript-internal::>integral
  ;

  \ Round a fixed-point number to the nearest integer with half rounding down
  : round-half-down ( fvalue -- nvalue )
    zscript-internal::double> forth::round-half-down zscript-internal::>integral
  ;

  \ Round a fixed-point number to the nearest integer with half rounding towards
  \ zero
  : round-half-zero ( fvalue -- nvalue )
    zscript-internal::double> forth::round-half-zero zscript-internal::>integral
  ;

  \ Round a fixed-point number to the nearest integer with half rounding away
  \ from zero
  : round-half-away-zero ( fvalue -- nvalue )
    zscript-internal::double> forth::round-half-away-zero zscript-internal::>integral
  ;

  \ Round a fixed-point number to the nearest integer with half rounding towards
  \ even
  : round-half-even ( fvalue -- nvalue )
    zscript-internal::double> forth::round-half-even zscript-internal::>integral
  ;

  \ Round a fixed-point number to the nearest integer with half rounding towards
  \ even
  : round-half-odd ( fvalue -- nvalue )
    zscript-internal::double> forth::round-half-odd zscript-internal::>integral
  ;

  \ Round a fixed-point number towards zero
  : round-zero ( fvalue -- nvalue )
    zscript-internal::double> forth::round-zero zscript-internal::>integral
  ;

  \ Round a fixed-point number away from zero
  : round-away-zero ( fvalue -- nvalue )
    zscript-internal::double> forth::round-away-zero zscript-internal::>integral
  ;

  \ Exponentation of a fixed point number by an unsigned integer
  : fi** ( fvalue0 uvalue -- fvalue1 )
    zscript-internal::integral> swap zscript-internal::double> rot
    forth::fi** zscript-internal::>double
  ;

  \ Compute the symmetric modulus of two S13.32 fixed point numbers
  : fmod ( fvalue0 fvalue1 -- fvalue2 )
    zscript-internal::2double> forth::fmod zscript-internal::>double
  ;

  \ Calculate a square root
  : sqrt ( fvalue0 -- fvalue1 )
    zscript-internal::double> forth::sqrt zscript-internal::>double
  ;

  \ Calculate a factorial
  : factorial ( uvalue -- udvalue )
    zscript-internal::integral> forth::factorial zscript-internal::>double
  ;

  \ Calculate (e^x)-1
  : expm1 ( fvalue0 -- fvalue1 )
    zscript-internal::double> forth::expm1 zscript-internal::>double
  ;

  \ Calculate e^x
  : exp ( fvalue0 -- fvalue1 )
    zscript-internal::double> forth::exp zscript-internal::>double
  ;

  \ Calculate ln(x + 1)
  : lnp1 ( fvalue0 -- fvalue1 )
    zscript-internal::double> forth::lnp1 zscript-internal::>double
  ;

  \ Calculate ln(x)
  : ln ( fvalue0 -- fvalue1 )
    zscript-internal::double> forth::ln zscript-internal::>double
  ;

  \ Calculate sin(x)
  : sin ( fvalue0 -- fvalue1 )
    zscript-internal::double> forth::sin zscript-internal::>double
  ;

  \ Calculate cos(x)
  : cos ( fvalue0 -- fvalue1 )
    zscript-internal::double> forth::cos zscript-internal::>double
  ;

  \ Calculate atan(x)
  : atan ( fvalue0 -- fvalue1 )
    zscript-internal::double> forth::atan zscript-internal::>double
  ;

  \ Calculate a angle for any pair of x and y coordinates
  : atan2 ( fy fx -- fangle )
    zscript-internal::2double> forth::atan2 zscript-internal::>double
  ;

  \ Calculate asin(x)
  : asin ( fvalue0 -- fvalue1 )
    zscript-internal::double> forth::asin zscript-internal::>double
  ;
  
  \ Calculate acos(x)
  : acos ( fvalue0 -- fvalue1 )
    zscript-internal::double> forth::acos zscript-internal::>double
  ;
  
  \ Calculate a fixed point power b^x
  : f** ( fb fx -- fb^x )
    zscript-internal::2double> forth::f** zscript-internal::>double
  ;
  
  \ Calculate sinh(x)
  : sinh ( fvalue0 -- fvalue1 )
    zscript-internal::double> forth::sinh zscript-internal::>double
  ;

  \ Calculate cosh(x)
  : cosh ( fvalue0 -- fvalue1 )
    zscript-internal::double> forth::cosh zscript-internal::>double
  ;

  \ Calculate tanh(x)
  : tanh ( fvalue0 -- fvalue1 )
    zscript-internal::double> forth::tanh zscript-internal::>double
  ;

  \ Calculate asinh(x)
  : asinh ( fvalue0 -- fvalue1 )
    zscript-internal::double> forth::asinh zscript-internal::>double
  ;

  \ Calculate acosh(x)
  : acosh ( fvalue0 -- fvalue1 )
    zscript-internal::double> forth::acosh zscript-internal::>double
  ;

  \ Calculate atanh(x)
  : atanh ( fvalue0 -- fvalue1 )
    zscript-internal::double> forth::atanh zscript-internal::>double
  ;

  \ Parse an S31.32 fixed-point number
  : parse-fixed ( bytes -- f64 success? )
    unsafe::bytes>addr-len zscript-internal::2integral> forth::parse-fixed
    5 cells ensure
    zscript-internal::>integral -rot zscript-internal::>double swap
  ;

  \ Parse a double number
  : parse-double ( bytes -- dvalue success? )
    unsafe::bytes>addr-len zscript-internal::2integral> forth::parse-double
    5 cells ensure
    zscript-internal::>integral -rot zscript-internal::>double swap
  ;

  \ Format an S31.32 fixed-point number
  : format-fixed { f64 -- bytes }
    65 make-bytes { bytes }
    bytes unsafe::bytes>addr-len drop zscript-internal::integral>
    f64 zscript-internal::double> forth::format-fixed
    nip zscript-internal::>integral
    0 swap bytes >slice duplicate
  ;

  \ Format a truncated S31.32 fixed-point number
  : format-fixed-truncate { f64 places -- bytes }
    33 places + make-bytes { bytes }
    bytes unsafe::bytes>addr-len drop zscript-internal::integral>
    f64 zscript-internal::double> places zscript-internal::integral>
    forth::format-fixed-truncate nip zscript-internal::>integral
    0 swap bytes >slice duplicate
  ;

  \ Format an signed double number
  : format-double { nd64 -- bytes }
    65 make-bytes { bytes }
    bytes unsafe::bytes>addr-len drop zscript-internal::integral>
    nd64 zscript-internal::double> forth::format-double nip
    zscript-internal::>integral
    0 swap bytes >slice duplicate
  ;

  \ Format an unsigned double number
  : format-double-unsigned { ud64 -- bytes }
    64 make-bytes { bytes }
    bytes unsafe::bytes>addr-len drop zscript-internal::integral>
    ud64 zscript-internal::double> forth::format-double-unsigned nip
    zscript-internal::>integral
    0 swap bytes >slice
  ;

  \ Type a double number without a following space
  : (d.) ( dvalue -- ) zscript-internal::double> forth::(d.) ;

  \ Type a double number with a following space
  : d. ( dvalue -- ) zscript-internal::double> forth::d. ;

  \ Type an S31.32 fixed-point number without a following space
  : (f.) ( fvalue -- ) zscript-internal::double> forth::(f.) ;

  \ Type an S31.32 fixed-point number with a following space
  : f. ( fvalue -- ) zscript-internal::double> forth::f. ;

  \ Type a truncated S31.32 fixed-point number without a following space
  : (f.n) ( fvalue fplaces -- )
    zscript-internal::integral> swap zscript-internal::double> rot forth::(f.n)
  ;

  \ Type a truncated S31.32 fixed-point number with a following space
  : f.n ( fvalue fplaces -- )
    zscript-internal::integral> swap zscript-internal::double> rot forth::f.n
  ;

  begin-module zscript-double-internal

    \ Handle a number
    : do-handle-number { addr bytes -- flag }
      addr bytes saved-handle-number-hook@ execute not if
        addr bytes unsafe::2>integral addr-len>bytes
        dup [: [char] . = ;] any if
          zscript-double::parse-double if
            state? if lit, then true
          else
            drop false
          then
        else
          zscript-double::parse-fixed if
            state? if lit, then true
          else
            drop false
          then
        then
      else
        true
      then
    ;
    
  end-module> import
  
  \ Initialize zscript-double
  : init-zscript-double ( -- )
    handle-number-hook@ saved-handle-number-hook!
    ['] zscript-double-internal::do-handle-number handle-number-hook!
  ;

  initializer init-zscript-double

end-module