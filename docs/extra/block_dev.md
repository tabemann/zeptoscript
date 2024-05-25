# Block devices

zeptoscript comes with modules for interacting with block devices. Currently only SDHC/SDXC cards communicated with over SPI are supported. These devices provide storage in the form of discrete fixed-sized blocks of storage at indices. Typically they provide caching of blocks for the purpose of speeding up repeated reads to the same blocks.

## `zscript-block-dev` words

This module is provided by `src/common/extra/block_dev.fs`.

The following words are available:

### `block-size`
( device -- bytes )

Get the size of a *device*'s blocks in bytes.

### `block-count`
( device -- count )

Get the number of blocks for *device*.

### `block!`
( data index device -- )

Write *data* to block *index* of *device*. *data* has to be of the size of a block.

### `block-part!`
( data offset index device -- )

Write *data* to block *index* of *device* starting at *offset*. The length of *data* plus *offset* cannot be greater than the size of a block.

### `block@`
( data index device -- )

Write block *index* of *device* to *data*. *data* has to be the size of a block.

### `block-part@`
( data offset index device -- )

Write block *index* of *device* starting at *offset* to *data*. The length of *data* plus *offset* cannot be greater than the size of a block.

### `flush-blocks`
( device -- )

Flush cached blocks for *device* to the underlying media.

### `clear-blocks`
( device -- )

Clear cached blocks for *device*.

### `write-through!`
( write-through device -- )

Set the *write-through* cache mode for *device*. When this is set to `true` writes to blocks are immediately written to the underlying media, while when this is set to `false` writes to blocks are merely cached unless an access forces a cached block to be evicted.

### `write-through@`
( device -- write-through )

Get the *write-through* cache mode for *device*.

### `x-block-out-of-range`
( -- )

Block out of range exception.

## `zscript-sdcard` module

This module is provided by `src/common/extra/sdcard.fs` and is dependent upon the `zscript-block-dev` module.

This module has the following words:

### `make-sd`
( cs-pin spi-device -- sd )

Construct an SDHC/SDXC card object with chip select pin *cs-pin* and SPI peripheral *spi-device*. These objects implement all the *device* methods specified in `zscript-block-dev`.

### `init-sd`
( sd -- )

Initialize the SDHC/SDXC card. This must be called prior to using the SDHC/SDXC card.

### `write-sd-block-zero!`
( enabled sd -- )

Set whether block zero of an SDHC/SDXC card is protected.
