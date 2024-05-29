# Filesystem tools

In addition to the raw filesystem API, zeptoscript also comes with optional filesystem tools to ease working with filesystems. They enable things such as executing code in files, listing directories, dumping files, creating and writing to files, creating directories, removing and renaming files and directories, and so on.

The filesystem tools are provided by the `zscript-fs-tools` module, which is provided by `src/common/extra/fs_tools.fs`.

Be aware that some of these words, such as `create-file` and `write-file`, share names with words in the `zscript-fs` module. As a result, one may want to refer to words in the `zscript-fs` module explicitly with `zscript-fs::` if one wants to use them, unless one is defining a module and has specifically imported the `zscript-fs` module and not th e `zscript-fs-tools` module.

## `zscript-fs-tools` module

The following words are in this module:

### `current-fs!`
( fs -- )

Set the current filesystem.
  
### `current-fs@`
( -- fs )

Get the current filesystem.
  
### `load-file`
( file -- )

Load an already-open file and execute it. Note that the file will be closed afterwards.
  
### `included`
( path -- )

Execute a file specified as path in a byte sequence.
  
### `include`
( "path" -- )

Execute a file specified as a token.

### `list-dir`
( path -- )

List a directory.

### `create-dir`
( path -- )

Create a directory.

### `copy-file`
( path new-path -- )

Copy a file.

### `append-file`
( data path -- )

Append to a file.

### `write-file-window`
( data offset path -- )

Write data at an offset in a file without truncating it.

### `write-file`
( data path -- )

Overwrite a file and then truncate it afterwards.

### `create-file`
( data path -- )

Create a file.

### `dump-file-raw`
( path -- )

Dump the contents of a file to the console as raw data.
  
### `dump-file-raw-window`
( offset length path -- )

Dump the contents of a window in a file to the console as raw data.

### `dump-file`
( path -- )

Dump the contents of a file to the console as bytes plus ASCII.

### `dump-file-window`
( offset length path -- )

Dump the contents of a window in a file to the console as bytes plus ASCII.

### `dump-file-ascii`
( path -- )

Dump the contents of a file to the console as ASCII.
  
### `dump-file-ascii-window`
( offset length path -- )

Dump the contents of a window in a file to the console as ASCII.

### `read-file`
( buffer offset path -- count )

Read a file, from an offset from the start of the file, to a fixed-sized.

### `file-size@`
( path -- size )

Get the size of a file.
  
### `remove-file`
( path -- )

Remove a file.

### `remove-dir`
( path -- )

Remove a directory.

### `rename`
( path new-name -- )

Rename a file.

### `exists?`
( path -- exists? )

Get whether a file or directory at a given path exists.

### `file?`
( path -- file? )

Get whether a directory entry is a file.

### `dir?`
( path -- dir? )

Get whether a directory entry is a directory.

### `x-fs-not-set`
( -- )

Filesystem not set exception.
