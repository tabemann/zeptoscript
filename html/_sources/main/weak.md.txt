# Weak references

Weak references are values that embody references which do not protect referenced values from garbage collection, and are aware of when said referenced values are garbage collected. A value that is only weakly referenced, i.e. its chains of references at some points contain only weak references, is guaranteed to be garbage collected at the next garbage collection cycle. In this way they differ from Java "soft" references, which are may be followed for garbage collection purposes except when memory is tight.

There are two types of weak references, single weak references and weak pairs. Single weak references are simply references to individual values. Weak pairs are pairs of a weak reference to a value and a strong reference to another value. The utility of weak pairs is that they enable constructing linked lists of weak references.

## `zscript-weak` words

### `broken-weak`
( -- symbol )

This symbol is a special value which represents a broken weak reference. Note that setting a weak reference to this value will not have the intended effect, because then `weak-broken?` will return `false` for a weak reference.

### `weak?`
( x -- flag )

Get whether a value is a single weak reference or a weak pair.

### `weak-pair?`
( x -- flag )

Get whether a value is a weak pair.

### `>weak`
( x -- weak )

Construct a single weak reference for a value *x*.

### `>weak-pair`
( x y -- weak-pair )

Construct a weak pair with a weak reference to *x* and a strong reference to *y*.

### `weak@`
( weak -- x )

Get the weakly referenced value for a weak reference; if the weak reference is broken, return `broken-weak`.

### `weak!`
( x weak -- )

Set the weakly referenced value for a weak reference. This can "repair" a broken weak reference.

### `weak-broken?`
( weak -- flag )

Get whether a weak reference is broken.

### `weak-pair-tail@`
( weak-pair -- tail )

Get the "tail", i.e. the strong reference, of a weak pair.

### `weak-pair-tail!`
( tail weak-pair -- )

Set the "tail", i.e. the strong reference, of a weak pair.
