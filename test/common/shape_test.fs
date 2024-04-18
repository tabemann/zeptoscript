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

begin-module shape-test

  zscript-oo import
  zscript-double import
  
  \ Invalid size
  : x-invalid-size ( -- ) ." invalid size" cr ;
  
  method describe ( shape -- description )
  method area ( shape -- area )
  method radius ( shape -- radius }
  method edge ( self -- edge )
  method width ( shape -- width )
  method height ( shape -- height )
  
  begin-class circle
    
    member: circle-radius
    
    :method new { radius self -- }
      radius d0> averts x-invalid-size
      radius self circle-radius!
    ;
    
    :method describe { self -- description }
      s" circle: radius="
      self radius format-fixed
      s"  area="
      self area format-fixed
      s"  "
      5 >cells 0bytes join
    ;
    
    :method area { self -- area }
      self radius pi f*
    ;
    
    :method radius { self -- radius }
      self circle-radius@
    ;
    
    :method width { self -- width }
      self radius 2,0 f*
    ;
    
    :method height { self -- height }
      self height 2,0 f*
    ;
    
  end-class
  
  begin-class square
    
    member: square-edge
    
    :method new { edge self -- }
      edge d0> averts x-invalid-size
      edge self square-edge!
    ;
    
    :method describe { self -- description }
      s" square: edge="
      self edge format-fixed
      s"  area="
      self area format-fixed
      s"  "
      5 >cells 0bytes join
    ;
    
    :method area { self -- area }
      self edge 2 fi**
    ;
    
    :method edge { self -- radius }
      self square-edge@
    ;
    
    :method width { self -- width }
      self edge
    ;
    
    :method height { self -- height }
      self edge
    ;
    
  end-class
  
  begin-class rectangle
    
    member: rectangle-width
    member: rectangle-height
    
    :method new { width height self -- }
      width d0> averts x-invalid-size
      height d0> averts x-invalid-size
      width self rectangle-width!
      height self rectangle-height!
    ;
    
    :method describe { self -- description }
      s" rectangle: width="
      self width format-fixed
      s"  height="
      self height format-fixed
      s"  area="
      self area format-fixed
      s"  "
      7 >cells 0bytes join
    ;
    
    :method area { self -- area }
      self width self height f*
    ;
    
    :method width { self -- width }
      self rectangle-width@
    ;
    
    :method height { self -- height }
      self rectangle-height@
    ;
    
  end-class
  
end-module

      