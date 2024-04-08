# zeptoscript Core Functionality

zeptoscript is a high-level, dynamically-typed language implemented on top of zeptoforth. It includes automatic memory management achieved through the use of a Cheney's algorithm garbage collector.

zeptoscript has the following basic data types:

* Integrals, with subtypes being nulls, small integers (i.e. 31-bit), and big integers (i.e. 32-bit).
* Cell sequences
* Cell slices
* Byte sequences, with normal and constant subtypes
* Byte slices
* Execution tokens, with normal and closure subtypes

All three subtypes of integrals are handled transparently to the user, and can only be told apart when the user applies the word `>type` to them. However, in most cases the number 0 will be a null, all integers that can be unambiguously represented as two's-complement numbers with 31 bits will be a small integer, and all integers that require a full 32 bits to be unambiguously represented will be a big integer. A key difference between nulls and small integers on one hand and bit integers on the other is that nulls and small integers are not allocated in the heap while big integers are; note that big integers require two cells to be stored for their heap allocation (in addition to any cells taken for references to them) while nulls and small integers only require one cell each.

Cell sequences, cell slices, non-constant byte sequences, and byte slices which have non-constant byte sequences as their backing store are mutable. Examples of words which may mutate these include `!+` (for cell sequences and slices), and `c!+`, `h!+`, and `w!+` (for byte sequences and slices). Other examples include `map!`, `mapi!`, `zip!`, `zip3!`, `reverse!`, and `sort!`, which transform sequences and slices in place. A general naming convention for mutating words is that they have `!` in their names.

The difference between sequences and slices is that sequences comprise a *backing store* whereas slices are references to an offset and length within a sequence. Consequently, mutating a slice mutates its underlying sequence and any other slices which share part of the same data within the same underlying sequence. In many cases they are more efficient to use rather than physically duplicating a sequence or a part of one; however, if one wants to create a separate sequence (which may be mutated without mutating the original sequence) from a sequence or slice one should use `duplicate` ( *seq* - *seq'* ), or another word that creates a new sequence from an existing one with a transformation, such as `map`, `mapi`, `filter`, `filteri`, `zip`, `zip3`, `reverse`, or `sort`.

Many of the operations work on all of cell sequences, cell slices, byte sequences (both normal and constant), and byte slices. However, there are exceptions; e.g. `@+`, `!+`, and `cells>` are only for cell sequences and slices while `c@+`, `c!+`, `h@+`, `h!+`, `w@+`, `w!+`, `bytes>`, and `type` are only for byte sequences.

Execution tokens and closures are closely related. Both execution tokens and closures may be executed with the words `execute`, `?execute`, and `try`; however, only execution tokens may be used as exceptions with `?raise`. The words `;]`, `'`, and `[']` create execution tokens, which reference code outside the zeptoscript heap which may be executed. The word `bind` ( *xn* ... *x0* *count* *xt* -- *closure* ) takes an execution token *xt* and binds it to *count* values on the stack, producing a *closure*, which when executed will push values *xn* ... *x0* onto the stack prior to executing *xt*. Both execution tokens and closures live on the heap. Note, though, that there may be cases where one may need to convert between execution tokens and integrals referencing the addresses of their underlying code; these conversions are achieved with `unsafe::integral>xt` ( *integral* -- *xt* ) and `unsafe::xt>integral` ( *xt* -- *integral* ). These conversions may not be made with closures.

One feature of zeptoscript is *records*. *Records* are essentially syntactic sugar on top of cell sequences which provides accessor words for constructing, setting, and accessing cell sequences and their elements. In many cases it reads to more readable code than using `>cells`, `cells>`, `@+`, and `!+` directly when one is using a cell sequence for purposes other than acting as a vector/array.

Take the following for example:

```
begin-record foo
  item: foo-x
  item: foo-y
  item: foo-z
end-record
```

This creates the following words:

* A constructor word `make-foo` ( -- *foo* ) for creating an empty (i.e. all null) record with three elements.
* A constructor word `>foo` ( *foo-x* *foo-y* *foo-z* -- *foo* ) for creating a record containing the specified elements popped off the stack
* An exploding word `foo>` ( *foo* -- *foo-x* *foo-y* *foo-z* ) which takes a record *foo* and pushes its elements onto the stack.
* A word `foo-size` ( -- size ) which pushes the size in elements of a *foo* record, in this case three.
* The words `foo-x@` ( *foo* -- *foo-x* ), `foo-y@` ( *foo* -- *foo-y* ), and `foo-z@` ( *foo* -- *foo-z* ) for fetching named elements of *foo*.
* The words `foo-x!` ( *foo-x* *foo* -- ), `foo-y!` ( *foo-y* *foo* -- ), and `foo-z!` ( *foo-z* *foo* -- ) for setting named elements of *foo*.

Before zeptoscript can be used it must be initialized with `zscript::init-zscript` ( *compile-size* *runtime-size* -- ). If this is done during compilation to flash *compile-size* is used for the heap size, otherwise *runtime-size* is used. Also, if this is done during compilation to flash an `init` routine is compiled to flash which initializes the heap to *runtime-size* next time the system is booted. Note that *compile-size* and *runtime-size* are rounded up to the nearest full multiple of eight bytes. All code to be compiled which is not zeptoscript code must be compiled before this point, as `zscript::init-zscript` overrides the numeric parser and thus will break non-zeptoscript code. Also, all code to be compiled which is zeptoscript code must be compiled after this point, as it will break otherwise (as compiling zeptoscript code outside of `src/common/core.fs` requires an initialized zeptoscript heap and numeric parser).

zeptoscript code may only execute in one task, because the zeptoscript garbage collector is only aware of the current task, and the synchronization necessary to achieve execution across tasks would prove problematic. However, for asynchronous programming, an *action scheduler* mechanism is provided by `src/common/action.fs`. This is similar to the zeptoforth action scheduler, but has a few minor differences (e.g. data transferred between actions is not copied aside from the copying of integral messages and references to allocated messages). The action scheduler is "stackless" in that every action shares the same underlying data and return stacks, and storing state information between states is accomplished through providing closures for each state, and in said closures referencing outside data.

S15.16 fixed-point numerics are provided by `src/common/fixed32.fs`. These fixed-point numbers are cell-sized so they fit in the space of normal integrals. Note that operations for addition, subtraction, and negation are not included in `src/common/fixed32.fs`. This is because normal integral `+`, `-`, and `negate` fits these roles. Note that loading `src/common/fixed32.fs` overrides the numeric parser to enable parsing S15.16 literals, which have the form `x;y` where `;` is the decimal point.

## Words

### `null-type`
( -- type )

The null type.

### `int-type`
( -- type )

The small integer type.

### `bytes-type`
( -- type )

The non-constant byte sequence type.

### `word-type`
( -- type )

The big integer type.

### `2word-type`
( -- type )

The double-cell type (for future use).

### `const-bytes-type`
( -- type )

The constant byte sequence type.

### `xt-type`
( -- type )

The execution token type.

### `tagged-type`
( -- type )

A type for auxiliary types, such as words.

### `cells-type`
( -- type )

The cell sequence type.

### `closure-type`
( -- type )

The closure type.

### `slice-type`
( -- type )

The slice type; note that this encompasses slices of cell sequences, non-constant byte sequences, and constant byte sequences.

### `raw-lit,`
( x -- )

Compile an integral value as a raw literal which, when executed, will push its value as a zeptoforth integral value rather than a zeptoscript integral value.

### `lit,`
( x -- )

Compile an integral value as a literal which, when executed, will push its value as a zeptoscript integral value.

### `addr-len>const-bytes`
( c-addr len -- const-bytes )

Create a constant byte sequence from a non-volatile address and length; this is intended for string literals compiled into code.

### `addr-len>bytes`
( c-addr len -- bytes )

Create a non-constant byte sequence from a potentially-volatile address and length; when creating byte sequences from raw data which is not compiled into code this is normally what you would want.

### `>raw`
( x -- x' )

Get the raw backing store of a cell sequence, byte sequence, or slice value. The primary purpose of this is for converting slices into their underlying cell or byte sequences.

### `>raw-offset`
( x -- offset )

Get the raw offset of a cell sequence, byte sequence, or slice value. The primary purpose of this is for determining where a slice points into its underlying cell or byte sequence.

### `>len`
( x -- len )

Get the length of a cell sequence, byte sequence, or slice in entries or bytes.

### `>pair`
( x0 x1 -- pair )

Create a two-element cell sequence from a pair of values.

### `pair>`
( pair -- x0 x1 )

Explode a two-element cell sequence or slice into a pair of values.

### `>triple`
( x0 x1 x2 -- triple )

Create a three-element cell sequence from three values.

### `triple>`
( triple -- x0 x1 x2 )

Explode a three-element cell sequence or slice into three values.

### `make-cells`
( count -- cells )

Create a cell sequence with the specified element count that is initialized to null.

### `make-bytes`
( count -- bytes )

Create a byte sequence with the specified byte count that is initialized to null.
### `>cells`
( xn ... x0 count -- cells )

Create a cell sequence with *count* elements taken off the stack.

### `>bytes`
( cn ... c0 count -- bytes )

Create a byte sequence with *count* bytes taken off the stack.

### `cells>`
( cells -- xn ... x0 count )

Explode a cell sequence or slice into elements and a count on the stack.

### `bytes>`
( bytes -- cn ... c0 count )

Explode a byte sequence or slice into bytes and a count on the stack.

### `cells-no-count>`
( cells -- xn ... x0 )

Explode a cell sequence or slice into elements on the stack.

### `bytes-no-count>`
( bytes -- cn ... c0 )

Explode a byte sequence or slice into bytes on the stack.

### `@+`
( index cells -- x )

Get the element at *index*, indexed from zero, of a cell sequence or slice.

### `!+`
( x index cells -- )

Set the elememt at *index*, indexed from zero, of a cell sequence or slice.

### `w@+`
( index bytes -- x )

Get the cell at *index*, indexed from zero and word-aligned, of a byte sequence or slice.

### `w!+`
( x index bytes -- )

Set the cell at *index*, indexed from zero and word-aligned, of a non-constant byte sequence or slice.

### `h@+`
( index bytes -- h )

Get the halfword at *index*, indexed from zero and halfword-aligned, of a byte sequence or slice.

### `h!+`
( h index bytes -- )

Set the halfword at *index*, indexed from zero and halfword-aligned, of a non-constant byte sequence or slice.

### `c@+`
( index bytes -- c )

Get the byte at *index*, indexed from zero, of a byte sequence or slice.

### `c!+`
( c index bytes -- )

Set the byte at *index*, indexed from zero, of a non-constant byte sequence or slice.

### `x@+`
( index cells|bytes -- x )

Get the element or byte at *index*, indexed from zero, of a cell or byte sequence or slice.

### `x!+`
( x index cells|bytes -- )

Set the elememt or byte at *index*, indexed from zero, of a non-constant cell or byte sequence or slice.

### `cells?`
( x -- cells? )

Get whether a value is a cell sequence or slice.

### `bytes?`
( x -- bytes? )

Get whether a value is a byte sequence or slice.

### `init-zscript`
( compile-size runtime-size -- )

Initialize zeptoscript. All non-zeptoscript code must be compiled before this word is called, and all zeptoscript code must be compiled after this word is called. If called during compilation to flash, a call to this word will be compiled into the initialization process on bootup, so zeptoscript will automatically be initialized after the previous definition of `init` (or declaration of an `initializer`) and before any subsequent definitions of `init` (or declarations of `initializer`). Note that when compiling to flash the zeptoscript heap size will be initialized to *compile-size*, but when compiling to RAM or on any subsequent boot the zeptoscript heap size will be initialized to *runtime-size*. Note that the heap size is rounded up to the nearest eight bytes.

### `copy`
( value0 offset0 value1 offset1 count -- )

Do a shallow copy of *count* elements or bytes from one cell or byte sequence or slice to another, starting at *offset0* of *value0* to *offset1* of *value1*. All offsets are indexed from zero. *value0* and *value1* must both be either cell sequences or slices or byte sequences or slices; copying between cell sequences or slices and byte sequences or slices is not permitted. Also note that *value1* cannot be a constant byte sequence or slice.

### `s"`
( "string" -- bytes )

Create a string literal as a byte sequence; note that if compiled the resulting byte sequence is constant, whereas if immediate the resulting byte sequence is non-constant.

### `s\"`

Create a string literal with escapes as a byte sequence; note that if compiled the resulting byte sequence is constant, whereas if immediate the resulting byte sequence is non-constant.

### `+`
( x0 x1 -- x2 )

Add two integers.

### `1+`
( x0 -- x1 )

Add one to an integer.

### `cell+`
( x0 -- x1 )

Add a cell, i.e. four, to an integer.

### `-`
( x0 x1 -- x2 )

Subtract two integers.

### `1-`
( x0 x1 -- x2 )

Subtract one from an integer.

### `*`
( x0 x1 -- x2 )

Multiply two integers.

### `2*`
( x0 -- x1 )

Multiply an integer by two.

### `4*`
( x0 -- x1 )

Multiply an integer by four.

### `cells`
( x0 -- x1 )

Multiply an integer by a cell.

### `/`
( n0 n1 -- n2 )

Divide two signed integers.

### `2/`
( n0 -- n1 )

Divide a signed integer by two.

### `4/`
( n0 -- n1 )

Divide a signed integer by four.

### `u/`
( u0 u1 -- u2 )

Divide two unsigned integers.

### `mod`
( n0 n1 -- n2 )

Get the modulus of two signed integers.

### `umod`
( u0 u1 -- u2 )

Get the modulus of two unsigned integers.

### `negate`
( n0 -- n1 )

Negate an integer.
  
### `or`
( x0 x1 -- x2 )

Or two integers.

### `and`
( x0 x1 -- x2 )

And two integers.

### `xor`
( x0 x1 -- x2 )

Exclusive-or two integers.

### `bic`
( x0 x1 -- x2 )

Clear bits in an integer.    

### `not`
( x0 -- x1 )

Not an integer.

### `invert`
( x0 -- x1 )

Invert an integer.

### `lshift`
( x0 x1 -- x2 )

Left shift an integer.

### `rshift`
( x0 x1 -- x2 )

Logical right shift an integer.

### `arshift`
( x0 x1 -- x2 )

Arithmetic right shift an integer.

### `align`
( x0 x1 -- x2 )

Align a value to a power of two.

### `=`
( x0 x1 -- flag )

Get whether two values are equal. Integral values are compared for their values, even in the case of big integers. Execution tokens are compared for their underlying code addresses. All other values are compared for their addresses in memory.

### `<>`
( x0 x1 -- flag )

Get whether two values are unequal. Integral values are compared for their values, even in the case of big integers. Execution tokens are compared for their underlying code addresses. All other values are compared for their addresses in memory.

### `<`
( n0 n1 -- flag )

Signed less than.

### `>`
( n0 n1 -- flag )

Signed greater than.

### `<=`
( n0 n1 -- flag )

Signed less than or equal.

### `>=`
( n0 n1 -- flag )

Signed greater than or equal.

### `u<`
( u0 u1 -- flag )

Unsigned less than.

### `u>`
( u0 u1 -- flag )

Unsigned greater than.

### `u<=`
( u0 u1 -- flag )

Unsigned less than or equal.

### `u>=`
( u0 u1 -- flag )

Unsigned greater than or equal.

### `u<`
( u0 u1 -- flag )

Unsigned less than.

### `u>`
( u0 u1 -- flag )

Unsigned greater than.

### `u<=`
( u0 u1 -- flag )

Unsigned less than or equal.

### `u>=`
( u0 u1 -- flag )

Unsigned greater than or equal.

### `0=`
( x -- flag )

Equal to zero; note that for non-integral values `false` is returned.

### `0<>`
( x -- flag )

Not equal to zero; note that for non-integral values `true` is returned.

### `0<`
( n -- flag )

Less than zero.

### `0>`
( n -- flag )

Greater than zero.

### `0<=`
( n -- flag )

Less than or equal to zero.

### `0>=`
( n -- flag )

Greater than or equal to zero.

### `min`
( n0 n1 -- n2 )

Get the minimum of two numbers.

### `max`
( n0 n1 -- n2 )

Get the maximum of two numbers.

### `cell`
( -- u )

The size of a cell, i.e. four.

### `if`
( x -- )

If conditional; note that non-integral values are treated as true.

### `while`
( x -- )

While conditional; note that non-integral values are treated as true.

### `until`
( x -- )

Until conditional; note that non-integral values are treated as true.

### `?do`
( end start -- )

Start `do` loop; note that this is redefined because of the chance that two vales may be equal but have different types.

### `loop`
( -- )

Close a `do` loop.

### `+loop`
( increment -- )

Close a `do` loop with an increment.

### `case`
( x -- )

Start a `case` block.

### `of`
( x -- )

Start an `of` block.

### `.`
( n -- )

Print a signed integer with a following space.

### `u.`
( u -- )

Print an unsigned integer with a following space.

### `(.)`
( n -- )

Print a signed integer without a following space.

### `(u.)`
( u -- )

Print an unsigned integer without a following space.

### `emit`
( c -- )

Print a character.

### `h.1`
( x -- )

Print the lowest four bits of an unsigned integer as one hexadecimal digit.

### `h.2`
( x -- )

Print the lowest eight bits of an unsigned integer as two hexadecimal digits.

### `h.4`
( x -- )

Print the lowest sixteen bits of an unsigned integer as four hexadecimal digits.

### `h.8`
( x -- )

Print an unsigned integer as eight hexadecimal digits.

### `h.16`
( d -- )

Print a double cell unsigned integer as sixteen hexadecimal digits.

### `spaces`
( x -- )

Print *x* spaces.

### `>slice`
( offset length seq -- slice )

Get an arbitrary slice of a sequence or slice of *length* starting at *offset*.

### `truncate-start`
( count seq -- slice )

Get a slice of a sequence or slice truncating *count* elements or bytes from the start.

### `truncate-end`
( count seq -- slice )

Get a slice of a sequence or slice truncating *count* elements or bytes from the end.

### `>type`
( x -- type )

Get the type of a value.

### `integral?`
( x -- integral? )

Get whether a value is integral.

### `small-int?`
( x -- small-int? )

Get whether a value is a small integer or null.

### `token`
( runtime: "name" -- seq | 0 )

Parse a token and return it as a byte sequence, or return 0 if no token could be parsed.

### `token-word`
( runtime: "name" -- word )

Parse a token and get the corresponding word; note that an exception will be raised if there is no token or the word cannot be found.

### `word>xt`
( word -- xt )

Get an execution token corresponding to a word.

### `;]`
( -- xt )

End a quotation (also known as a lambda) and, if compiling, compile pushing the execution token, or otherwise immediately pushing the execution token.

### `execute`
( xt | closure -- )

Execute an execution token or closure. If a closure is executed, push the bound values onto the stack prior to executing the underlying execution token.

### `try`
( xt | closure -- exception | 0 )

Execute an execution token or closure like `execute`, but catching any exceptions that are raised. If an exception is raised it is returned, otherwise 0 is returned.

### `?execute`
( xt | closure | 0 -- )

If a non-zero value is provided it is executed like with `execute`, otherwise it is ignored.

### `type`
( seq -- )

Type a string.
  
### `'`
( "name" -- xt )

Get an xt at interpretation-time.

### `[']`
( 'name" -- xt )

Get an xt at compile-time.

### `?raise`
( xt -- )

Raise an exception.

### `averts`
( f "name" -- )

Assert that a value is true, otherwise raise a specified exception.

### `triggers`
( f "name" -- )

Assert that a value is false, otherwise raise a specified exception.

### `raise`
( "name" -- )

Always raise an exception; this is needed due to issues with compiling code.

### `start-compile`
( seq -- )

Start compiling a word with a name.

### `end-compile,`
( -- )

End compiling, exported to match start-compile.

### `constant-with-name`
( x name -- )

Define a constant with a name.

### `[char]`
( "name" -- x )

Redefine [CHAR].

### `char`
( "name" -- x )

Redefine CHAR.

### `begin-record`
( "name" -- token offset )

Begin declaring a record.

### `end-record`
( token offset -- )

Finish declaring a record.

### `item:`
( offset "name" -- offset' )

Create a field in a record.

### `foreign-constant`
( "foreign-name" "new-name" -- )

Make a foreign constant.

### `foreign-variable`
( "foreign-name" "new-name" -- )

Make a foreign variable.

### `foreign`
( in-count out-count "foreign-name" "new-name" -- )

Make a foreign word usable.

### `execute-foreign`
( in-count out-count xt -- )

Execute a foreign word.

### `find`
( seq -- word|0 )

Find a word.

### `state?`
state forth::@ >integral ;

Get the compilation state.
  
### `+to`
( x "name" -- )

Add to a local or a VALUE.

### `constant`
( x "name" -- )

Define a CONSTANT.

### `2constant`
( x0 x1 "name" -- )

Define a 2CONSTANT.

### `global`
( "name" -- )

Create a global.

### `bind`
( xn ... x0 count xt -- closure )

Bind a scope to a lambda.

### `pick`
( xn ... x0 u -- x )

Redefine PICK.

### `roll`
( xn ... x0 u -- xn-1 ... x0 xn u )

Redefine ROLL.

### `base!`
( base -- )

Set BASE.

### `base@`
( -- base )

Get BASE.

### `with-base`
( base xt -- )

Execute an xt with a BASE.

### `parse-integer`
( seq -- n success )

Parse an integer.

### `duplicate`
( seq0 -- seq1 )

Duplicate a cell or byte sequence; this converts slices to non-slices and constant byte sequences into non-constant byte sequences.

### `concat`
( seq0 seq1 -- seq2 )

Concatenate two cell or byte sequences.

### `iter`
( seq xt -- )

Iterate over a cell sequence.

### `iteri`
( seq xt -- )

Iterate over a cell sequence with an index.

### `map`
( seq xt -- seq' )

Map a cell or byte sequence into a new cell sequence.

### `mapi`
( seq xt -- seq' )

Map a cell or byte sequence into a new cell sequence with an index.

### `map!`
( seq xt -- )

Map a cell or byte sequence in place.

### `mapi!`
( seq xt -- )

Map a cell or byte sequence in place with an index.

### `make-bits`
( len -- bits )

Make a zeroed bit sequence.

### `bits>len`
( bits -- len )

Get the length of bits.

### `bit!`
( bit index bits -- )

Set a bit in a bit sequence.

### `bit@`
( index bits -- )

Get a bit in a bit sequence.

### `filter`
( seq xt -- seq' )

Filter a cell or byte sequence.

### `filteri`
( seq xt -- seq' )

Filter a cell or byte sequence with an index.

### `foldl`
( x seq xt -- x' ) xt: ( x item -- x' )

Fold left over a cell or byte sequence.

### `foldli`
( x seq xt -- x' ) xt: ( x item index -- x' )

Fold left over a cell or byte sequence with an index.

### `foldr`
( x seq xt -- x' ) xt: ( item x -- x' )

Fold right over a cell or byte sequence.

### `foldri`
( x seq xt -- x' ) xt: ( item x index -- x' )

Fold right over a cell or byte sequence with an index.

### `reverse`
( seq -- seq' )

Reverse a sequence producing a new sequence.

### `reverse!`
( seq -- )

Reverse a sequence in place.

### `zip`
( seq0 seq1 -- seq2 )

Zip two sequences into a new sequence, using the length of the shorter sequence.

### `zip3`
( seq0 seq1 seq2 -- seq3 )

Zip three sequences into a new sequence, using the length of the shorter sequence.

### `zip!`
( seq0 seq1 -- )

Zip two sequences into the first sequence in-place; note that if the ranges do not match an exception is raised, and the first sequence must be a cell sequence.

### `zip3!`
( seq0 seq1 seq2 -- )

Zip three sequences into the first sequence in-place; note that if the ranges do not match an exception is raised, and the first sequence must be a cell sequence.

### `sort!`
( seq xt -- )

Heapsort a cell or byte sequence in place.

### `sort`
( seq xt -- )

Heapsort a cell or byte sequence, copying it.

### `all`
( seq xt -- all? )

Get whether a predicate applies to all elements of a sequence; note that not all elements will be iterated over if an element returns false, and true will be returned if the sequence is empty.

### `any`
( seq xt -- any? )

Get whether a predicate applies to any element of a sequence; note that not all elements will be iterated over if an element returns true, and false will be returned if the sequence is empty.

### `join`
( list-seq join-seq -- seq' )

Join a cell sequence of cell or byte sequences.

### `depth`
( -- depth )

Get the current depth.

### `#(`
( -- )

Start defining a cell sequence.

### `#<`
( -- )

Start defining a byte sequence.

### `)#`
( xn ... x0 -- cells )

Finish definining a cell sequence.

### `>#`
( cn ... c0 -- bytes )

Finish defining a byte sequence.

### `0cells`
( -- cells )

Empty cell sequence.

### `0bytes`
( -- bytes )

Empty byte sequence.

## Unsafe words

### `bytes>addr-len`
( bytes -- addr len )

Get the starting address and length of a byte sequence or slice. Note that these are not guaranteed to remain constant with any subsequent allocations that may trigger the garbage collector.
    
### `@`
( addr -- x )

Redefine `@`.
  
### `!`
( x addr -- )

Redefine `!`.

### `+!`
( x addr -- )

Redefine `+!`.

### `bis!`
( x addr -- )

Redefine `BIS!`.
    
### `bic!`
( x addr -- )

Redefine `BIC!`.

### `xor!`
( x addr -- )

Redefine `XOR!`.

### `h@`
( addr -- h )

Redefine `H@`.
  
### `h!`
( h addr -- )

Redefine `H!`.
  
### `h+!`
( h addr -- )

Redefine `H+!`.

### `hbis!`
( x addr -- )

Redefine `HBIS!`.
    
### `hbic!`
( x addr -- )

Redefine `HBIC!`.

### `hxor!`
( x addr -- )

Redefine `HXOR!`.

### `c@`
( addr -- c )

Redefine `C@`.
  
### `c!`
( c addr -- )

Redefine `C!`.

### `c+!`
( c addr -- )

Redefine `C+!`.

### `cbis!`
( x addr -- )

Redefine `CBIS!`.
    
### `fill`
( addr bytes val -- )

Redefine `FILL`.

### `>integral`
( x -- value )

Cast a value to an integer.
    
### `integral>`
( value -- x )

Cast a value from an integer.

### `2>integral`
( x0 x1 -- value0 value1 )

Cast two values to integers.

### `2integral>`
( value0 value1 -- x0 x1 )

Cast two values from integers.

### `xt>integral`
( xt -- value )

Convert an xt to an integral.

### `integral>xt`
( value -- xt )

Convert an integral to an xt.

### `here`
( -- x )

Get the HERE pointer.

### `allot`
( x -- )

ALLOT space.