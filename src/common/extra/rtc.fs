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

begin-module zscript-rtc

  zscript-oo import
  zscript-special-oo import
  
  begin-module zscript-rtc-internal

    foreign-constant forth::rtc::date-time-size date-time-size

    defined? forth::rtc::date-time@ [if]
      1 0 foreign forth::rtc::date-time@ date-time@
    [then]

    defined? forth::rtc::date-time! [if]
      1 0 foreign forth::rtc::date-time@ date-time!
    [then]
    
    1 1 foreign forth::rtc::date-time-year date-time-year
    1 1 foreign forth::rtc::date-time-month date-time-month
    1 1 foreign forth::rtc::date-time-day date-time-day
    1 1 foreign forth::rtc::date-time-dotw date-time-dotw
    1 1 foreign forth::rtc::date-time-hour date-time-hour
    1 1 foreign forth::rtc::date-time-minute date-time-minute
    1 1 foreign forth::rtc::date-time-second date-time-second
    1 1 foreign forth::rtc::date-time-msec date-time-msec
    
    2 2 foreign forth::rtc::format-date-time format-date-time
    foreign-constant forth::rtc::max-date-time-format-size max-date-time-format-size

    1 0 foreign forth::rtc::update-dotw update-dotw
    
  end-module> import

  defined? date-time@ [if]
    \ Get date time
    method date-time@ ( self -- )
  [then]
  
  defined? date-time! [if]
    \ Set date time
    method date-time! ( self -- )
  [then]

  \ Set year
  method date-time-year! ( year self -- )

  \ Set month
  method date-time-month! ( month self -- )

  \ Set day
  method date-time-day! ( day self -- )

  \ Set day of the week
  method date-time-dotw! ( dotw self -- )

  \ Set hour
  method date-time-hour! ( hour self -- )

  \ Set minute
  method date-time-minute! ( minute self -- )

  \ Set second
  method date-time-second! ( second self -- )

  \ Set millisecond
  method date-time-msec! ( msec self -- )
  
  \ Get year
  method date-time-year@ ( self -- year )

  \ Get month
  method date-time-month@ ( self -- month )

  \ Get day
  method date-time-day@ ( self -- day )

  \ Get day of the week
  method date-time-dotw@ ( self -- dotw )

  \ Get hour
  method date-time-hour@ ( self -- hour )

  \ Get minute
  method date-time-minute@ ( self -- minute )

  \ Get second
  method date-time-second@ ( self -- second )

  \ Get millisecond
  method date-time-msec@ ( self -- msec )
  
  \ Update the day of the week for a date/time
  method update-dotw ( self -- )

  \ Date/time class
  begin-class date-time

    \ Date/time data
    member: date-time-data

    \ Constructor
    :method new { self -- }
      date-time-size make-bytes self date-time-data!
      [ defined? date-time@ ] [if]
        self date-time@
      [then]
    ;

    defined? date-time@ [if]
      \ Get date time
      :method date-time@ ( self -- )
        date-time-data@ unsafe::bytes>addr-len drop
        zscript-rtc-internal::date-time@
      ;
    [then]

    defined? date-time! [if]
      \ Set date time
      :method date-time! ( self -- )
        date-time-data@ unsafe::bytes>addr-len drop
        zscript-rtc-internal::date-time!
      ;
    [then]

    \ Set year
    :method date-time-year! ( year self -- )
      date-time-data@ unsafe::bytes>addr-len drop date-time-year unsafe::!
    ;

    \ Set month
    :method date-time-month! ( month self -- )
      date-time-data@ unsafe::bytes>addr-len drop date-time-month unsafe::c!
    ;

    \ Set day
    :method date-time-day! ( day self -- )
      date-time-data@ unsafe::bytes>addr-len drop date-time-day unsafe::c!
    ;

    \ Set day of the week
    :method date-time-dotw! ( dotw self -- )
      date-time-data@ unsafe::bytes>addr-len drop date-time-dotw unsafe::c!
    ;

    \ Set hour
    :method date-time-hour! ( hour self -- )
      date-time-data@ unsafe::bytes>addr-len drop date-time-hour unsafe::c!
    ;

    \ Set minute
    :method date-time-minute! ( minute self -- )
      date-time-data@ unsafe::bytes>addr-len drop date-time-minute unsafe::c!
    ;

    \ Set second
    :method date-time-second! ( second self -- )
      date-time-data@ unsafe::bytes>addr-len drop date-time-second unsafe::c!
    ;

    \ Set millisecond
    :method date-time-msec! ( msec self -- )
      date-time-data@ unsafe::bytes>addr-len drop date-time-msec unsafe::h!
    ;
    
    \ Get year
    :method date-time-year@ ( self -- year )
      date-time-data@ unsafe::bytes>addr-len drop date-time-year unsafe::@
    ;

    \ Get month
    :method date-time-month@ ( self -- month )
      date-time-data@ unsafe::bytes>addr-len drop date-time-month unsafe::c@
    ;

    \ Get day
    :method date-time-day@ ( self -- day )
      date-time-data@ unsafe::bytes>addr-len drop date-time-day unsafe::c@
    ;

    \ Get day of the week
    :method date-time-dotw@ ( self -- dotw )
      date-time-data@ unsafe::bytes>addr-len drop date-time-dotw unsafe::c@
    ;

    \ Get hour
    :method date-time-hour@ ( self -- hour )
      date-time-data@ unsafe::bytes>addr-len drop date-time-hour unsafe::c@
    ;

    \ Get minute
    :method date-time-minute@ ( self -- minute )
      date-time-data@ unsafe::bytes>addr-len drop date-time-minute unsafe::c@
    ;

    \ Get second
    :method date-time-second@ ( self -- second )
      date-time-data@ unsafe::bytes>addr-len drop date-time-second unsafe::c@
    ;

    \ Get millisecond
    :method date-time-msec@ ( self -- msec )
      date-time-data@ unsafe::bytes>addr-len drop date-time-msec unsafe::h@
    ;
    
    \ Update the day of the week for a date/time
    :method update-dotw ( self -- )
      date-time-data@ unsafe::bytes>addr-len drop
      zscript-rtc-internal::update-dotw
    ;

    \ Format the date/time as a string
    :method show { self -- bytes }
      max-date-time-format-size make-bytes { bytes }
      16 cells ensure
      bytes unsafe::bytes>addr-len drop
      self date-time-data@ unsafe::bytes>addr-len drop
      format-date-time nip dup max-date-time-format-size <> if
        bytes 0 rot >slice
      else
        drop bytes
      then
    ;

    \ Make a hash for a date/time
    :method hash ( self -- )
      date-time-data@ try-hash 1+
    ;

    \ Test two date/times for equality
    :method equal? { other self -- }
      other class@ self class@ = if
        other date-time-data@ self date-time-data@ try-equal?
      else
        false
      then
    ;
    
  end-class
  
end-module
