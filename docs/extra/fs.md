# Filesystems

zeptoscript has optional support for filesystems, specifically FAT32 filesystems on SDHC/SDXC cards. This support includes opening and creating files, reading and writing files, removing files, opening and creating directories, reading directory entries, removing directories, and renaming files and directories. Note that moving files is not supported due to the nature of the FAT32 filesystem.

There are three main modules concerned with filesystems ─ `zscript-fs` for providing abstract methods for accessing filesystems, `zscript-fat32` for providing access to the FAT32 filesystem itself, and `zscript-simple-fat32` for providing a simplified interface for accessing FAT32 filesystems. Additionally, the module `zscript-block-dev` (discussed elsewhere) exposes being able to set and get write-through on FAT32 filesytems. For most purposes one will only need to use the `zscript-fs` module except when creating a filesystem, for which one will typically only need the `zscript-simple-fat32` module.

The `zscript-fs` module is provided by `src/common/extra/fs.fs`. The `zscript-fat32` module is provided by `src/common/extra/fat32.fs`. The `zscript-simple-fat32` module is provided by `src/common/extra/simple_fat32.fs`.

## `zscript-fs` words

### `change-dir`
( dir -- )

Set the current directory.

### `flush`
( fs -- )

Flush a filesystem.

### `clone`
( file|dir -- file'|dir' )

Clone a file or directory object.

### `close`
( file|dir -- )

Close a file or directory.
  
### `read-file`
( buffer file -- bytes )

Read data from a file.
  
### `write-file`
( buffer file -- bytes )

Write data to a file.
  
### `truncate-file`
( file -- )

Truncate a file.
  
### `seek-file`
( offset whence file -- )

Seek in a file.
  
### `tell-file`
( file -- offset )

Get the current offset in a file.
  
### `file-size@`
( file -- bytes )

Get the size of a file.

### `with-file-input`
( xt file -- )

Redirect input within *xt* to come from *file*. Note that input is buffered, so if *file* is written to while input is redirected from it, the written data may not be reflected in the input data.

### `with-file-output`
( xt file -- )

Redirect output within *xt* to *file*. Note that output is buffered, so if *file* is read while output is redirected to it, the read data may not reflect the output data.

### `with-file-error-output`
( xt file -- )

Redirect error output within *xt* to *file*. Note that error output is buffered, so if *file* is read while output is redirected to it, the read data may not reflect the output data.

### `with-file-all-output`
( xt file -- )

Redirect both output and error output within *xt* to *file*. Note that output and error output are buffered, so if *file* is read while output and error output are redirected to it, the read data may not reflect the output data.

### `fs@`
( file|dir -- fs )

Get the filesystem of a file or directory.
  
### `read-dir`
( dir -- entry|0 entry-read? )

Read an entry from a directory, and return whether an entry was read.
  
### `create-file`
( path dir -- file )

Create a file.
  
### `open-file`
( path dir -- file )

Open a file.
  
### `remove-file`
( path dir -- )

Remove a file.
  
### `create-dir`
( path dir -- dir' )

Create a directory.
  
### `open-dir`
( path dir -- dir' )

Open a directory.
  
### `remove-dir`
( path dir -- )

Remove a directory.
  
### `rename`
( new-name path dir -- )

Rename a file or directory.
  
### `dir-empty?`
( dir -- empty? )

Get whether a directory is empty.

### `exists?`
( path dir -- exists? )

Get whether a directory entry exists.

### `file?`
( path dir -- file? )

Get whether a directory entry is a file.

### `dir?`
( path dir -- dir? )

Get whether a directory entry is a directory.

### `root-dir@`
( fs -- dir )

Get the filesystem root directory.

### `current-dir@`
( fs -- dir )

Get the filesystem current directory, which is the root directory if the current directory is not the given filesystem.

### `entry-file?`
( entry -- file? )

Get whether an entry is a file.
  
### `entry-dir?`
( entry -- dir? )

Get whether an entry is a directory.

### `name@`
( entry -- name )

Get an entry's file or directory name.

### `create-date-time@`
( entry -- date-time )

Get an entry's creation date and time.

### `modify-date-time@`
( entry -- date-time )

Get an entry's modification date and time.

### `entry-file-size@`
( entry -- size )

Get an entry's file size.

### `seek-set`
( -- whence )

Seek from the beginning of a file.

### `seek-cur`
( -- whence )

Seek from the current position in a file.

### `seek-end`
( -- whence )

Seek from the end of a file.

### `x-file-name-format`
( -- )

Unsupported file name format exception.

### `x-entry-not-found`
( -- )

Directory entry not found exception.

### `x-entry-already-exists`
( -- )

Directory entry already exists exception.

### `x-entry-not-file`
( -- )

Directory entry is not a file exception.

### `x-entry-not-dir`
( -- )

Directory entry is not a directory exception.

### `x-dir-is-not-empty`
( -- )

Directory is not empty exception.

### `x-forbidden-dir`
( -- )

Directory name being changed or set is forbidden exception.

### `x-empty-path`
( -- )

No file or directory referred to in path within directory exception.
  
### `x-invalid-path`
( -- )

Invalid path exception.

### `x-not-open`
( -- )

File or directory is not open exception.

### `x-shared-file`
( -- )

File is shared exception.

### `x-open`
( -- )

File or directory is open exception.

## `zscript-fat32` words

### `partition-active?`
( partition -- active? )

Is the partition really active?.

### `partition-active@`
( partition -- active )

Is the partition active.

### `partition-type@`
( partition -- type )

Get the partition type.

### `partition-first-sector@`
( partition -- first-sector )

Get the partition first sector.

### `partition-sectors@`
( partition -- sectors )

Get the partition sector count.

### `make-mbr`
( mbr-device -- mbr )

Construct a master boot record object.

### `mbr-valid?`
( mbr -- valid? )

Get whether the master boot record is valid.

### `partition@`
( index mbr -- partition )

Read a partition.

### `partition!`
( partition index mbr -- )

Write a partition.

### `make-fat32-fs`
( partition device -- fs )

Construct a FAT32 filesystem object.

### `x-sector-size-not-supported`
( -- )

Sector size exception.

### `x-fs-version-not-supported`
( -- )

Filesystem version not supported exception.
  
### `x-bad-info-sector`
( -- )

Bad info sector exception.
  
### `x-no-clusters-free`
( -- )

No clusters free exception.
  
### `x-file-name-format`
( -- )

Unsupported file name format exception.
  
### `x-out-of-range-entry`
( -- )

Out of range directory entry index exception.
  
### `x-out-of-range-partition`
( -- )

Out of range partition index exception.

### `x-no-end-marker`
( -- )

Directory with no end marker exception.

## `zscript-simple-fat32` words

### `make-simple-fat32-fs`
( sck-pin tx-pin rx-pin cs-pin spi-device -- fs )

Create a FAT32 filesystem for partition 0 of an SDHC/SDXC card communicated with over SPI with SPI peripheral *spi-device*, SCK pin *sck-pin*, TX pin *tx-pin*, RX pin *rx-pin*, and Chip Select pin *cs-pin*.
