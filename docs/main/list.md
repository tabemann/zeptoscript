# Lists

zeptoscript has optional support for lists composed of pairs. Internally each pair is a two-element cell sequence; while one can use cell sequence words with these, this is not recommended as it will generally give results different from what one might expect.

The particular use case of lists is to enable efficient prepending elements without reallocating and copying an entire sequence for each prepended or appended element; if appending is desired, the entire list can be efficiently reversed (and can be efficiently converted into a sequence simultaneously) afterwards.

It is also efficient in many cases to reverse a list while generating a new list from an existing one due to the nature of lists, or when generating a sequence from a list. However, this is less necessary than in some languages because zeptoscript is not purely functional, and hence can mutate lists as they are being enerated, avoiding the need for a subsequent reversing step to achieve in-order conversions. (Yes, Haskell also does not need a reversing step either, but that is through the magic of lazy evaluation.)

Note, however, that lists are less efficient memory-wise in static usage than sequences in most use cases (except possibly in use cases where multiple lists share parts of their structures) because each pair takes up three cells in the heap whereas each element of a cell sequence takes up only one cell in the heap in addition to the cell in the heap taken up by the cell sequence's header. As a result, it is often advisable to convert lists to sequences once one is done constructing them if they are not ephemeral in nature and provided there is enough space available in the heap to keep the list and the sequence in memory simultaneously (which is necessary for converting from one to the other).

## `zscript-list` Words

### `cons`
( x list -- list' )

Prepend *x* onto *list*, giving a new list sharing its tail with the original list.

### `empty`
( -- list )

An empty list. Note that this is equivalent to `false`, and is treated as such.

### `head@`
( list -- x )

Get the head of *list*.

### `tail@`
( list -- list' )

Get the tail of *list*.

### `head!`
( x list -- )

Set the head of *list* to *x*.

### `tail!`
( tail-list list -- )

Set the tail of *list* to *tail-list*.

### `empty?`
( list -- empty? )

Get whether *list* is empty.

### `last`
( list -- x )

Get the last element of *list*, or `empty` if the list is empty.

### `nth`
( index list -- x )

Get the the element of *list* at *index*, zero indexed, or `empty` if the list does not contain *index* + 1 elements.

### `nth-tail`
( index list -- list' )

Get the *index*-th tail of *list*, with zero returning all of *list*, or `empty` if there is no *index*-th tail of *list*.

### `list>len`
( list -- len )

Get the length of *list*; note that this involves a full traversal of *list* and hence is O(n).

### `list>cells`
( list -- cells )

Convert a list to a cell sequence. Note that this involves traversing *list* twice, the first time to get the length of *list*, and the second time to copy over each element.

### `list>bytes`
( list -- bytes )

Convert a list to a byte sequence. Note that this involves traversing *list* twice, the first time to get the length of *list*, and the second tiem to copy over each element.

### `seq>list`
( seq -- list )

Convert a cell or byte sequence or slice to a list.

### `iter-list`
( list xt -- ) xt: ( item -- )

Iterate over *list*, executing *xt* with each element in order from start to end.

### `iteri-list`
( list xt -- ) xt: ( item index -- )

Iterate over *list*, executing *xt* with each element and its index (indexed from zero) in order from start to end.

### `find-index-list`
( list xt -- index found? ) xt: ( item -- flag )

Get the index of an element of a list that meets a predicate; note that the lowest matching index is returned, and xt will not necessarily be called against all items.

### `find-indexi-list`
( list xt -- index found? ) xt: ( item index -- flag )

Get the index of an element of a list that meets a predicate with an index; note that the lowest matching index is returned, and xt will not necessarily be called against all items.

### `rev-map-list`
( list xt -- list' ) xt: ( item -- item' )

Map *xt* over *list* producing a new list in reverse order, executing *xt* with each element in order from start to end.

### `rev-mapi-list`
( list xt -- list' ) xt: ( item index -- item' )

Map *xt* over *list* producing a new list in reverse order, executing *xt* with each element and its index (indexed from zero) in order from start to end.

### `rev-filter-list`
( list xt -- list' ) xt: ( item -- flag )

Filter *list* with *xt* producing a new list in reverse order, executing *xt* with each element in order from start to end.

### `rev-filteri-list`
( list xt -- list' ) xt: ( item index -- filter )

Filter *list* with *xt* producing a new list in reverse order, executing *xt* with each element and its index (indexed from zero) in order from start to end.

### `rev-list>cells`
( list -- cells )

Convert *list* into a cell sequence in reverse order.

### `rev-list>bytes`
( list -- bytes )

Convert *list* into a byte sequence in reverse order.

### `rev-list`
( list -- list' )

Do a shallow copy of *list* as a list in reverse order.

### `map-list`
( list xt -- list' ) xt: ( item -- item' )

Map *xt* over *list* producing a new list, executing *xt* with each element in order from start to end.

### `mapi-list`
( list xt -- list' ) xt: ( item index -- item' )

Map *xt* over *list* producing a new list, executing *xt* with each element and its index (indexed from zero) in order from start to end.

### `map!-list`
( list xt -- ) xt: ( item -- item' )

Map *xt* over *list* mutating *list* in place, executing *xt* with each element in order from start to end.

### `mapi!-list`
( list xt -- ) xt: ( item index -- item' )

Map *xt* over *list* mutating *list* in place, executing *xt* with each element and its index (indexed from zero) in order from start to end.

### `filter-list`
( list xt -- list' ) xt: ( item -- flag )

Filter *list* with *xt* producing a new list, executing *xt* with each element in order from start to end.

### `filteri-list`
( list xt -- list' ) xt: ( item index -- filter )

Filter *list* with *xt* producing a new list, executing *xt* with each element and its index (indexed from zero) in order from start to end.

### `all-list`
( list xt -- all? )

Get whether a predicate applies to all elements of a list; note that not all elements will be iterated over if an element returns false, and true will be returned if the list is empty.

### `any-list`
( list xt -- any? )

Get whether a predicate applies to any element of a list; note that not all elements will be iterated over if an element returns true, and false will be returned if the list is empty.

### `foldl-list`
( x list xt -- x' ) xt: ( x item -- x' )

Fold left *xt* over *list* with *x* as an initial value, executing *xt* with each element along with the current value in order from start to end.

### `foldli-list`
( x list xt -- x' ) xt: ( x item index -- x' )

Fold left *xt* over *list* with *x* as an initial value, executing *xt* with each element along with the current value and its index (indexed from zero) in order from start to end.

### `foldr-list`
( x list xt -- x' ) xt: ( item x -- x' )

Fold right *xt* over *list* with *x* as an initial value, executing *xt* with each element along with the current value in roder from end to start. Note that this involves converting *list* to a cell sequence internally, so is less efficient than `foldl-list`.

### `foldri-list`
( x list xt -- x' ) xt: ( item x -- x' )

Fold right *xt* over *list* with *x* as an initial value, executing *xt* with each element along with the current value and its index (indexed from zero) in roder from end to start. Note that this involves converting *list* to a cell sequence internally, so is less efficient than `foldli-list`.

### `duplicate-list`
( list -- list' )

Do a shallow copy of a list.

### `>rev-list`
( xn ... x0 count -- list )

Create a list with *count* elements taken off the top of the stack, in reverse order.

### `>list`
( x0 ...  xn count -- list )

Create a list with *count* elements taken off the top of the stack, in order.

### `list>`
( list -- x0 ... xn count )

Explode a list onto the stack.
