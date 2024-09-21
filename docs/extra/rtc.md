# Real-time clock

zeptoscript has support for real-time clocks on the RP2040, and more basic support for date/time handling on other MCU's. This is encapsulated in the `date-time` class, which wraps the RTC and date/time functionality provided by zeptoforth. This includes getting and setting the real-time clock, calculating the day of the week, and converting dates into human-readable byte sequences.

## `zscript-rtc` module

### `make-date-time`
( -- date-time )

Construct a `date-time` instance, initializing it to the current date/time on the RP2040 and to all zeroes on other platforms.

### `date-time@`
( self -- )

Get date time. Note that this is only available on the RP2040.

### `date-time!`
( self -- )

Set date time. Note that this is only available on the RP2040.

### `date-time-year!`
( year self -- )

Set year.

### `date-time-month!`
( month self -- )

Set month.

### `date-time-day!`
( day self -- )

Set day.

### `date-time-dotw!`
( dotw self -- )

Set day of the week.

### `date-time-hour!`
( hour self -- )

Set hour.

### `date-time-minute!`
( minute self -- )

Set minute.

### `date-time-second!`
( second self -- )

Set second.

### `date-time-msec!`
( msec self -- )

Set millisecond.

### `date-time-year@`
( self -- year )

Get year.

### `date-time-month@`
( self -- month )

Get month.

### `date-time-day@`
( self -- day )

Get day.

### `date-time-dotw@`
( self -- dotw )

Get day of the week.

### `date-time-hour@`
( self -- hour )

Get hour.

### `date-time-minute@`
( self -- minute )

Get minute.

### `date-time-second@`
( self -- second )

Get second.

### `date-time-msec@`
( self -- msec )

Get millisecond.

### `update-dotw`
( self -- )

Update the day of the week for a date/time.

Also implemented are the following words from `zscript-special-oo`:

* `show`
* `hash`
* `equal?`
