# Sets

Sets are data structures for containing unique values in an unordered fashion. Values can be arbitrarily added to, removed from, and tested for presence in a set.

A very common sort of set is a string-set. The easiest way to implement a string-set is to create a set with the `hash-bytes` hash word and the `equal-bytes?` equality word, and shown below:

```
global my-set
0 ' hash-bytes ' equal-bytes? make-set my-set!
```

Values can be added to a set as shown below:

```
s" foo" my-set@ insert-set
s" bar" my-set@ insert-set
s" baz" my-set@ insert-set
s" qux" my-set@ insert-set
s" quux" my-set@ insert-set
```

Values can be removed from a set as shown below:

```
s" qux" my-set@ remove-set
```

Values can be tested for membership in a set as shown below:

```
s" foo" my-set@ in-set? .
```

This will output:

```
-1  ok
```

Non-membership (in this case after removing the value "qux") is shown by:

```
s" qux" my-set@ in-set? .
```

This will output:

```
0  ok
```

A set can be iterated over as shown below:

```
my-set@ [: type space ;] iter-set
```

This will output:

```
quux foo bar baz  ok
```

A set can be tested for any element matching a predicate as shown below:

```
my-set@ [: s" foo" equal-bytes? ;] any-set .
```

This will output:

```
-1  ok
```

No element matching a predicate is shown below:

```
my-set@ [: s" foobar" equal-bytes? ;] any-set .
```

This will output:

```
0  ok
```

A set can be tested for all elements matching a predicate as shown below:

```
my-set@ [: >len 5 < ;] any-set .
```

This will output:

```
-1  ok
```

Not all elements matching a predicate is shown below:

```
my-set@ [: >len 4 < ;] any-set .
```

This will output:

```
0  ok
```

While values may be mutable values, undefined results will occur if the values are mutated; if this may be an issue, it would be prudent to use `duplicate` (or its like) to duplicate the values before inserting them and/or after retrieving the values if they may be mutated afterward.

If `src/common/special_oo.fs` has been compiled after `src/common/set.fs`, `>generic-set` is available. It creates a set sized to the number of specified entries using `zscript-special-oo` words `hash` and `equal?` for members, also known as a *generic* set.

An example of its use is as follows:

```
s" foo" s" bar" s" baz" 3 >generic-set [: type space ;] iter-set
```

This outputs:

```
baz foo bar  ok
```

There is also syntactic sugar for creating generic sets, by placing members in `#|` ... `|#`. Its use can be seen as follows:

```
#| s" foo" s" bar" s" baz" |# [: type space ;] iter-set
```

This outputs:

```
baz foo bar  ok
```

## `zscript-set` words

### `make-set`
( size hash-xt equal-xt -- set ) hash-xt: ( value -- hash ) equal-xt: { value0 value1 -- equal? )

Make a set (a size of 0 indicates a default size). *hash-xt* is a hash function applied to each value for the set. *equal-xt* is a function to test the equality of two values for the set.

### `duplicate-set`
( set -- set' )

Duplicate a set. This generates a shallow copy of the set; the values themselves are not duplicated.

### `iter-set`
( set xt -- ) xt: ( value -- )

Iterate over the elements of a set.

### `any-set`
( set xt -- ) xt: ( value -- flag )

Get whether any element of a set meet a predicate.

### `all-set`
( set xt -- ) xt: ( value -- flag )

Get whether all elements of a set meet a predicate.

### `set>values`
( set -- values )

Get the values of a set as a cell sequence.

### `insert-set`
( value set -- )

Insert an entry in a set.

### `remove-set`
( value set -- )

Remove an entry from a set.

### `in-set?`
( value set -- found? )

Test for membership in a set.

The following words are only available if `src/common/special_oo.fs` has been compiled after `src/common/set.fs`:

### `generic-set`
( valn ... val0 count -- set )

Create a generic set, i.e. one using `zscript-special-oo::hash` for key hashes and `zscript-special-oo::equal?` for member equality using *count* values on the stack.

### `#|`
( -- )

Begin defining a generic set without specifying a count.

### `|#`
( valn .. val0 -- set )

Finish defining a generic set using members on the stack.
