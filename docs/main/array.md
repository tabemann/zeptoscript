# Arrays

zeptoscript has optional support for arrays. Arrays are like sequences (internally they simply wrap a sequence), but they may change length. They have four types, specifically cell arrays, cell array slices, byte arrays, and byte array slices.

Note that while arrays may be resized, repeated prepending of elements is still best accomplished with lists, and each time an array is concatenated a whole new underlying sequence is allocated and the elements from the original arrays are copied into it. However, arrays are more memory-efficient than lists, especially when long, as they consist simply of a backing sequence and a small wrapper.

## `zscript-array` Words

### `array?`
( x -- array? )

Get whether something is an array.

### `cell-array?`
( x -- cell-array? )

Get whether something is a cell array.

### `byte-array?`
( x -- byte-array? )

Get whether something is a byte array.

### `array-seq@`
( array -- seq )

Get the underlying sequence in an array.

### `array>len`
( array -- len )

Get the length of an array.

### `wrap-seq-as-array`
( seq -- array )

Wrap a sequence in an array.

### `>cell-array`
( x0 ... xn count -- array )

Create a cell array from the stack.

### `>byte-array`
( c0 ... cn count -- array )

Create a byte array from the stack.

### `array>`
( array -- x0 ... xn count )

Explode an array onto the stack.

### `#!`
( -- )

Begin defining a cell array.

### `!#`
( -- )

End defining a cell array.

### `#$`
( -- )

Begin defining a byte array.

### `$#`
( -- )

End defining a byte array.

### `array@`
( index array -- x )

Get an element of an array.

### `array!`
( x index array -- )

Set an element of an array.

### `duplicate-array`
( array -- array' )

Duplicate an array.

### `concat-arrays`
( array0 array1 -- array2 )

Concatenate two cell or byte arrays.

### `concat!-arrays`
( array0 array1 -- )

Concatenate two cell or byte arrays in place.

### `seq>array`
( seq -- array )

Convert a sequence to an array, duplicating it.

### `array>seq`
( array -- seq )

Convert an array to a sequence, duplicating it.

### `array>slice`
( offset count array -- array' )

Get a slice of an array.

### `array>slice!`
( offset count array -- )

Get a slice of an array in place.

### `truncate-start-array`
( count array -- array' )

Truncate the start of an array as a slice.

### `truncate-end-array`
( count array -- array' )

Truncate the end of an array as a slice.

### `truncate-start!-array`
( count array -- )

Truncate the start of an array in place as a slice.

### `truncate-end!-array`
( count array -- )

Truncate the end of an array in place as a slice.

### `iter-array`
( array xt -- )

Iterate over an array.

### `iteri-array`
( array xt -- )

Iterate over an array with an index.

### `find-index-array`
( array xt -- index found? ) xt: ( item -- flag )

Get the index of an element that meets a predicate; note that the lowest matching index is returned, and xt will not necessarily be called against all items.

### `find-indexi-array`
( array xt -- index found? ) xt: ( item index -- flag )

Get the index of an element that meets a predicate with an index; note that the lowest matching index is returned, and xt will not necessarily be called against all items.

### `map-array`
( array xt -- array' ) xt: ( item -- item' )

Map a cell or byte array into a new cell or byte array.

### `mapi-array`
( array xt -- array' ) xt: ( item -- item' )

Map a cell or byte array into a new cell or byte array with an index.

### `map!-array`
( array xt -- ) xt: ( item -- item' )

Map a cell or byte array in place.

### `mapi!-array`
( array xt -- ) xt: ( item -- item' )

Map a cell or byte array in place with an index.

### `filter-array`
( array xt -- array' ) xt: ( item -- item' )

Filter a cell or byte array into a new cell or byte array.

### `filteri-array`
( array xt -- array' ) xt: ( item -- item' )

Filter a cell or byte array into a new cell or byte array with an index.

### `filter!-array`
( array xt -- ) xt: ( item -- item' )

Filter a cell or byte array in place.

### `filteri!-array`
( array xt -- ) xt: ( item -- item' )

Filter a cell or byte array in place with an index.

### `foldl-array`
( x array xt -- x' ) xt: ( x item -- x' )

Fold left over a cell or byte array.

### `foldli-array`
( x array xt -- x' ) xt: ( x item index -- x' )

Fold left over a cell or byte array with an index.

### `foldr-array`
( x array xt -- x' ) xt: ( item x -- x' )

Fold right over a cell or byte array.
  
### `foldri`
( x array xt -- x' ) xt: ( item x index -- x' )

Fold right over a cell or byte array with an index.

### `collectl-cell-array`
( x len xt -- array ) xt: ( x -- x item )

Collect elements of a cell array from left to right.

### `collectli-cell-array`
( x len xt -- array ) xt: ( x index -- x item )

Collect elements of a cell array from left to right with an index.

### `collectr-cell-array`
( x len xt -- array ) xt: ( x -- x item )

Collect elements of a cell array from right to left.

### `collectri-cell-array`
( x len xt -- array ) xt: ( x -- x item )

Collect elements of a cell array from right to left with an index.

### `collectl-byte-array`
( x len xt -- array ) xt: ( x -- x item )

Collect elements of a byte array from left to right.

### `collectli-byte-array`
( x len xt -- array ) xt: ( x index -- x item )

Collect elements of a byte array from left to right with an index.

### `collectr-byte-array`
( x len xt -- array ) xt: ( x -- x item )

Collect elements of a byte array from right to left.

### `collectri-byte-array`
( x len xt -- array ) xt: ( x -- x item )

Collect elements of a byte array from right to left with an index.

### `reverse-array`
( array -- array' )

Reverse an array producing a new array.

### `reverse!-array`
( array -- )

Reverse an array in place.

### `zip-arrays`
( array0 array1 -- array2 )

Zip two arrays into a new array, using the length of the shorter array.

### `zip3-arrays`
( array0 array1 array2 -- array3 )

Zip three arrays into a new array, using the length of the shorter array.

### `zip!-arrays`
( array0 array1 -- )

Zip two arrays into the first array in-place, using the length of the shorter array.

### `zip3!-arrays`
( array0 array1 array2 -- )

Zip three arrays into the first array in-place, using the length of the  shorter array.

### `sort!-array`
( array xt -- )

Heapsort an array in place.

### `sort-array`
( array xt -- array' )

Heapsort an array, copying it.

### `all-array`
( array xt -- all? ) xt: ( element -- match? )

Get whether a predicate applies to all elements of a array; note that  not all elements will be iterated over if an element returns false, and true will be returned if the array is empty.
  
### `alli-array`
( array xt -- all? ) xt: ( element index -- match? )

Get whether a predicate applies to all elements of a array; note that not all elements will be iterated over if an element returns false, and true will be returned if the array is empty.

### `any-array`
( array xt -- any? ) xt: ( element -- match? )

Get whether a predicate applies to any element of a array; note that not all elements will be iterated over if an element returns true, and false will be returned if the array is empty.

### `anyi-array`
( array xt -- any? ) xt: ( element index -- match? )

Get whether a predicate applies to any element of a array; note that not all elements will be iterated over if an element returns true, and false will be returned if the array is empty.

### `split-array`
( array xt -- array' ) xt: ( item -- flag )

Split an array based on a predicate.

### `spliti-array`
( array xt -- array' ) xt: ( item index -- flag )

Split an array based on a predicate with an index.

### `split!-array`
( array xt -- ) xt: ( item -- flag )

Split an array in place based on a predicate.

### `spliti!-array`
( array xt -- ) xt: ( item index -- flag )

Split an array in place based on a predicate with an index.

### `join-arrays`
( list-array join-array -- array' )

Join an array of arrays.
