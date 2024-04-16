# Maps

Maps (not to be confused with `map` words) are data structures for associating keys with values. Values can be inserted into maps at keys, values can be removed from maps at keys, and values at keys can be removed from maps.

A very common sort of map is a string-map. The easiest way to implement a string-map is to create a map with the `hash-bytes` hash word and the `equal-bytes?` equality word, and shown below:

```
global my-map
0 ' hash-bytes ' equal-bytes? make-map my-map!
```

`hash-bytes` and `equal-bytes?` are provided by `zscript`; `hash-bytes` is a 32-bit FNV-1 hash function.

To insert a value `256` at the key `foo`, one can do the following:

```
256 s" foo" my-map@ insert-map
```

To get the value of the key `foo`, one can do the following:

```
s" foo" my-map@ find-map . .
```

This outputs:

```
-1 256  ok
```

This indicates that the key existed and had the value `256`.

To attempt to get the value of a non-existent key `bar`, one can do the following:

```
s" bar" my-map@ find-map . .
```

This outputs:

```
0 0  ok
```

This indicates that the key did not exist.

To simply test for the existence of the key `foo`, without getting its value, one can do the following:

```
s" foo" my-map@ in-map? .
```

This outputs:

```
-1  ok
```

This indicates that the key did exist.

For another test, to iterate through each of the key/value pairs in a map, one can do:

```
0 ' hash-bytes ' equal-bytes? make-map my-map!
10 s" foo" my-map@ insert-map
20 s" bar" my-map@ insert-map
30 s" baz" my-map@ insert-map
```

followed by:

```
my-map@ [: type space . ;] iter-map
```

This outputs:

```
bar 20 baz 30 foo 10  ok
```

To test any of the elements of the map, one can do:

```
my-map@ [: drop 20 < ;] any-map .
```

This outputs:

```
-1  ok
```

Or one can do:

```
my-map@ [: drop 50 >= ;] any-map .
```

This outputs:

```
0  ok
```

To test all of the elements of the map, one can do

```
my-map@ [: drop 0> ;] all-map .
```

This outputs:

```
-1  ok
```

Or one can do:

```
my-map@ [: drop 20 = ;] all-map .
```

This outputs:

```
0  ok
```

One can get and display all the keys in the map with:

```
my-map@ map-keys [: type space ;] iter
```

This outputs:

```
bar baz foo  ok
```

One can also get and display all the values in the map with:

```
my-map@ map-values ' . iter
```

This outputs:

```
20 30 10  ok
```

One can also get and display all the key-value pairs in the map with:

```
my-map@ map-key-values [: pair> swap type space . ;] iter
```

This outputs:

```
bar 20 baz 30 foo 10  ok
```

While keys may be mutable values, undefined results will occur if the keys are mutated; if this may be an issue, it would be prudent to use `duplicate` (or its like) to duplicate the keys before inserting them and/or after retrieving the keys if they may be mutated afterward.

## `zscript-map` words

### `make-map`
( size hash-xt equal-xt -- map ) hash-xt: ( key -- hash ) equal-xt: { key0 key1 -- equal? )

Make a map (a size of 0 indicates a default size). *hash-xt* is a hash function applied to each key for the map. *equal-xt* is a function to test the equality of two keys for the map.

### `duplicate-map`
( map -- map' )

Duplicate a map. This generates a shallow copy of the map; the keys and values themselves are not duplicated.

### `iter-map`
( map xt -- ) xt: ( value key -- )

Iterate over the elements of a map.

### `map-map`
( map xt -- map' ) xt: ( value key -- value' )

Map over a map and create a new map with identical keys but new values.

### `map!-map`
( map xt -- ) xt: ( value key -- value' )

Map over a map and mutate its values in place.

### `any-map`
( map xt -- ) xt: ( value key -- flag )

Get whether any element of a map meet a predicate.

### `all-map`
( map xt -- ) xt: ( value key -- flag )

Get whether all elements of a map meet a predicate.

### `map-keys`
( map -- keys )

Get the keys of a map as a cell sequence.

### `map-values`
( map -- values )

Get the values of a map as a cell sequence.

### `map-key-values`
( map -- pairs )

Get the keys and values of a map as pairs as a cell sequence.

### `insert-map`
( val key map -- )

Insert an entry in a map.

### `remove-map`
( key map -- )

Remove an entry from a map.
  
### `find-map`
( key map -- val found? )

Find an entry in a map.

### `in-map?`
( key map -- found? )

Test for membership in a map.
