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

begin-module zscript-fixed32

  zscript import
  
  \ The saved handle-number-nook
  global saved-handle-number-hook

  \ The handle-number-hook foreign variable
  foreign-variable forth::handle-number-hook handle-number-hook

  \ Multiply two S15.16 fixed-point numbers
  2 1 foreign fixed32::f32* f32* ( x y -- z )

  \ Divide an S15.16 fixed-point number by another
  2 1 foreign fixed32::f32/ f32/ ( x y -- z )

  \ Convert an S15.16 fixed-point number to a 16-bit integer
  1 1 foreign fixed32::f32>s f32>s ( x -- y )

  \ Convert a 16-bit integer to an S15.16 fixed-point number
  1 1 foreign fixed32::s>f32 s>f32 ( x -- y )

  \ Convert an S31.32 fixed-point number to an S15.16 fixed-point number
  2 1 foreign fixed32::f64>f32 f64>f32 ( D: x -- y )

  \ Convert an S15.16 fixed-point number to an S31.32 fixed-point number
  1 2 foreign fixed32::f32>f64 f32>f64 ( x -- D: y )
  
  \ Calculate the modulus of two S15.16 fixed-point numbers
  2 1 foreign fixed32::f32mod f32mod ( x y -- z )

  \ Get the ceiling of an S15.16 fixed-point number
  1 1 foreign fixed32::f32ceil f32ceil ( f32 -- n )

  \ Get the floor of an S15.16 fixed-point number
  1 1 foreign fixed32::f32floor f32floor ( f32 -- n )

  \ Round an S15.16 fixed-point number to the nearest integer with half
  \ rounding up
  1 1 foreign fixed32::f32round-half-up f32round-half-up ( f32 -- n )

  \ Round an S15.16 fixed-point number to the nearest integer with half
  \ rounding down
  1 1 foreign fixed32::f32round-half-down f32round-half-down
  ( f32 -- n )

  \ Round a S15.16 fixed-point number to the nearest integer with half
  \ rounding towards zero
  1 1 foreign fixed32::f32round-half-zero f32round-half-zero
  ( f32 -- n )
  
  \ Round a S15.16 fixed-point number to the nearest integer with half
  \ rounding away from zero
  1 1 foreign fixed32::f32round-half-away-zero f32round-half-away-zero
  ( f32 -- n )
  
  \ Round a S15.16 fixed-point number to the nearest integer with half
  \ rounding towards even
  1 1 foreign fixed32::f32round-half-even f32round-half-even
  ( f32 -- n )
  
  \ Round a S15.16 fixed-point number to the nearest integer with half
  \ rounding towards even
  1 1 foreign fixed32::f32round-half-odd f32round-half-odd ( f32 -- n )
  
  \ Round a S15.16 fixed-point number towards zero
  1 1 foreign fixed32::f32round-zero f32round-zero ( f32 -- n )
  
  \ Round a S15.16 fixed-point number away from zero
  1 1 foreign fixed32::f32round-away-zero f32round-away-zero
  ( f32 -- n )

  \ Pi as a S15.16 fixed-point number
  foreign-constant fixed32::f32pi f32pi

  \ Get the square root of an S15.16 fixed-point number
  1 1 foreign fixed32::f32sqrt f32sqrt ( x -- y )
  
  \ Exponentiate an S15.16 fixed-point number by an integer
  2 1 foreign fixed32::f32i** f32i** ( f32 exponent -- f32' )

  \ Get the (e^x)-1 of an S15.16 fixed-point number
  1 1 foreign fixed32::f32expm1 f32expm1 ( f32 -- f32' )
  
  \ Get the e^x of an S15.16 fixed-point number
  1 1 foreign fixed32::f32exp f32exp ( f32 -- f32' )
  
  \ Get the ln(x+1) of an S15.16 fixed-point number
  1 1 foreign fixed32::f32lnp1 f32lnp1 ( f32 -- f32' )
  
  \ Get the ln(x) of an S15.16 fixed-point number
  1 1 foreign fixed32::f32ln f32ln ( f32 -- f32' )
  
  \ Get the sine of an S15.16 fixed-point number
  1 1 foreign fixed32::f32sin f32sin ( f32 -- f32' )
  
  \ Get the cosine of an S15.16 fixed-point number
  1 1 foreign fixed32::f32cos f32cos ( f32 -- f32' )
  
  \ Get the tangent of an S15.16 fixed-point number
  1 1 foreign fixed32::f32tan f32tan ( f32 -- f32' )
  
  \ Get the arctangent of an S15.16 fixed-point number
  1 1 foreign fixed32::f32atan f32atan ( f32 -- f32' )
  
  \ Get the angle of an x and an y S15.16 fixed-point numbers
  2 1 foreign fixed32::f32atan2 f32atan2 ( f32x f32y -- f32angle )
  
  \ Get the arcsine of an S15.16 fixed-point number
  1 1 foreign fixed32::f32asin f32asin ( f32 -- f32' )
  
  \ Get the arccosine of an S15.16 fixed-point number
  1 1 foreign fixed32::f32acos f32acos ( f32 -- f32' )
  
  \ Exponentiate two S15.16 fixed-point numbers
  2 1 foreign fixed32::f32** f32** ( f32b f32x -- f32b^f32x )

  \ Get the hyperbolic sine of an S15.16 fixed-point number
  1 1 foreign fixed32::f32sinh f32sinh ( f32 -- f32' )
  
  \ Get the hyperbolic cosine of an S15.16 fixed-point number
  1 1 foreign fixed32::f32cosh f32cosh ( f32 -- f32' )

  \ Get the hyperbolic tangent of an S15.16 fixed-point number
  1 1 foreign fixed32::f32tanh f32tanh ( f32 -- f32' )

  \ Get the hyperbolic arcsine of an S15.16 fixed-point number
  1 1 foreign fixed32::f32asinh f32asinh ( f32 -- f32' )

  \ Get the hyperbolic arccosine of an S15.16 fixed-point number
  1 1 foreign fixed32::f32acosh f32acosh ( f32 -- f32' )

  \ Get the hyperbolic arctangent of an S15.16 fixed-point number
  1 1 foreign fixed32::f32atanh f32atanh ( f32 -- f32' )

  begin-module zscript-fixed32-internal

    \ Parse an S15.16 fixed-point number
    2 2 foreign fixed32::parse-f32 parse-f32
    ( c-addr bytes -- f32 success? )

    \ Format an S15.16 fixed-point number
    2 2 foreign fixed32::format-f32 format-f32
    ( c-addr f32 -- c-addr bytes )

    \ Format a truncated S15.16 fixed-point number
    3 2 foreign fixed32::format-f32-truncate format-f32-truncate
    ( c-addr f32 places -- c-addr bytes )
    
  end-module

  \ Parse an S15.16 fixed-point number
  : parse-f32 ( bytes -- f32 success? )
    unsafe::bytes>addr-len zscript-fixed32-internal::parse-f32
  ;

  \ Format an S15.16 fixed-point number
  : format-f32 { f32 -- bytes }
    33 make-bytes { bytes }
    bytes unsafe::bytes>addr-len drop f32
    zscript-fixed32-internal::format-f32 nip { len }
    0 len bytes >slice
  ;

  \ Format truncated S15.16 fixed-point number
  : format-f32-truncate { f32 places -- bytes }
    17 places + make-bytes { bytes }
    bytes unsafe::bytes>addr-len drop f32 places
    zscript-fixed32-internal::format-f32-truncate nip
    { len }
    0 len bytes >slice
  ;

  \ Type an S15.16 fixed-point number without a following space
  1 0 foreign fixed32::(f32.) (f32.) ( f32 -- )

  \ Type a truncated S15.16 fixed-point number without a following space
  2 0 foreign fixed32::(f32.n) (f32.n) ( f32 places -- )

  \ Type an S15.16 fixed-point number with a following space
  1 0 foreign fixed32::f32. f32. ( f32 -- )

  \ Type a truncated S15.16 fixed-point number with a following space
  2 0 foreign fixed32::f32.n f32.n ( f32 places -- )

  continue-module zscript-fixed32-internal

    \ Handle a number
    : do-handle-number { addr bytes -- flag }
      addr bytes saved-handle-number-hook@ unsafe::integral>xt execute not if
        addr bytes unsafe::2>integral addr-len>bytes
        zscript-fixed32::parse-f32 if
          state? if lit, then true
        else
          drop false
        then
      else
        true
      then
    ;
    
  end-module
  
  \ Initialize zscript-fixed32
  : init-zscript-f32 ( -- )
    handle-number-hook@ saved-handle-number-hook!
    ['] zscript-fixed32-internal::do-handle-number
    unsafe::xt>integral handle-number-hook!
  ;

  initializer init-zscript-f32
  
end-module
