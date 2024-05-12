# Bitmaps, fonts, and displays

zeptoscript comes with functionality for drawing on and copying between bitmaps and displays and for drawing characters and text on bitmaps with fonts.

## `zscript-bitmap` words

### `op-set`

Set pixels operation.

### `op-or`

Or pixels operation.

### `op-and`

And pixels operation.

### `op-bic`

Bit-clear pixels operation

### `op-xor`

Exclusive-or pixels operation

### `make-bitmap`
( cols rows -- bitmap )

Create a bitmap with *cols* and *rows*.

### `clear-dirty`
( bitmap -- )

Clear the dirty state of a bitmap.

### `dirty?`
( bitmap -- dirty? )

Get the dirty state of a bitmap.

### `dirty-rect@`
( bitmap -- start-col start-row end-col end-row )

Get the dirty rectangle of a bitmap.

### `dim@`
( bitmap -- cols rows )

Get the dimensions of a bitmap.

### `clear-bitmap`
( bitmap -- )

Clear a bitmap.

### `back-bitmap@`
( bitmap -- bitmap' )

Get the real backing bitmap for an object implementing the bitmap API.

### `pixel@`
( col row bitmap -- state )

Get whether a pixel at *col* and *row* in a bitmap is set.

### `draw-pixel-const`
( pattern dst-col dst-row op dst-bitmap -- )

Draw *pattern* as an 8-bit vertical pattern using *op* on *dst-bitmap* at *dst-col* and *dst-row*.

### `draw-rect-const`
( pattern start-dst-col start-dst-row cols rows op dst-bitmap -- )

Draw *pattern* as an 8-bit vertical pattern using *op* on *dst-bitmap* as a rectangle starting at *start-dst-col* and *start-dst-row* of *cols* and *rows* size.

### `draw-rect`
( start-src-col start-src-row start-dst-col start-dst-row cols rows op src-bitmap dst-bitmap -- )

Draw *cols* by *rows* pixels from *start-src-col* and *start-src-row* in *src-bitmap* to *start-dst-col* and *start-dst-row* in *dst-bitmap*.

### `copy-pixels`
( col page dst-address count bitmap -- )

Copy *count* bytes of *bitmap* starting at *col* and an eight-bit-high *page* (i.e. the row divided by eight) to a buffer at *dst-address*.

## `zscript-bitmap-utils` words

### `draw-pixel-line`
( pattern x0 y0 x1 y1 op dst-bitmap -- )

Draw a line from *x0* and *y0* to *x1* and *y1* on *dst-bitmap* with *op* and *pattern*.
  
### `draw-rect-line`
( pattern width height x0 y0 x1 y1 op dst-bitmap -- )

Draw a rectangle to *dst-bitmap* with *op* and *pattern* along a line from *x0* and *y0* to *x1* and *y1*.

### `draw-bitmap-line`
( src-x src-y width height x0 y0 x1 y1 op src-bitmap dst-bitmap -- )

Apply a *src-x* and *src-y* of *src-bitmap* of size *width* and *height* to *dst-bitmap* with *op* and *pattern* along a line from *x0* and *y0* to *x1* and *y1*.

### `draw-pixel-circle`
( pattern x y radius op dst-bitmap -- )

Draw an empty circle of *radius* centered at *x* and *y*  on a bitmap with *op* and *pattern*.

### `draw-rect-circle`
( pattern width height x y radius op dst-bitmap -- )

Draw an empty circle of *radius* centered at *x* and *y* on a bitmap with a constant rectangle of *width* and *height* with *op* and *pattern*.
  
### `draw-bitmap-circle`
( src-x src-y width height x y radius op src-bitmap dst-bitmap -- )

Draw an empty circle of *radius* centered at *x* and *y* on *dst-bitmap* with *src-x* and *src-y* of *src-bitmap* of size *width* and *height* as a brush.

### `draw-filled-circle`
( pattern x y radius op dst-bitmap -- )

Draw an filled circle of *radius* centered at *x* and *y* on a bitmap with a rectangle operation.

## `zscript-font` words

### `make-font`
( default-char cols rows min-char max-char -- font )

Create a font with characters of *cols* by *rows* with a default character *default-char* and minimum and maximum characters *min-char* and *max-char*.

### `char-row!`
( xn ... x0 row c font -- )

Set *row* in a character *c* of *font*.

### `draw-char`
( c col row op bitmap font -- )

Draw with *font* a character *c* onto *bitmap* at *col* and *row* with *op*.
  
### `draw-string`
( bytes col row op bitmap font -- )

Draw with *font* a string *bytes* onto *bitmap* at *col* and *row* with *op*.

### `char-dim@`
( c font -- cols rows )

Get the dimensions in pixels in *font* of a character *c*.

### `string-dim@`
( bytes self -- cols rows )

Get the dimensions in pixels in *font* of a string *bytes*.

## `zscript-simple-font` words

### `make-simple-font`
( -- font )

Make a simple font.

Also implemented by the `simple-font` class are the following words in `zscript-font`:

* `draw-char`
* `draw-string`
* `char-dim@`
* `string-dim@`

## `zscript-ssd1306` words

### `make-ssd1306`
( pin0 pin1 cols rows i2c-addr i2c-device -- ssd1306 )

Create an SSD1306 device at *i2c-addr* on *i2c-device* using I2C pins *pin0* and *pin1* (their identities do not matter) of size *cols by *rows*. Note that the display is not blanked by default.

### `display-contrast!`
( contrast ssd1306 -- )

Set the contrast, from 0 to 255, of *ssd1306*.

### `update-display`
( ssd1306 -- )

Update *ssd1306* with the dirty contents of its framebuffer and clear its dirty state.

Also implemented by the `ssd1306` class are the following words in `zscript-bitmap`:

* `dim@`
* `clear-bitmap`
* `back-bitmap@`
* `pixel@`
* `draw-pixel-const`
* `draw-rect-const`
* `draw-rect`
