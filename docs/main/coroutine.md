# Asymmetric coroutines

zeptoscript has optional support for *asymmetric* coroutines, as contrasted with zeptoscript tasks, which are *symmetric* coroutines. Asymmetric coroutines are explicitly suspended and resumed, and in particular specific coroutines are resumed, with data being passed to them, and when coroutines are suspended they transfer both control and data back to what had resumed them.

Coroutines start out in a *suspended* state, and only suspended coroutines can be resumed. Coroutines stay in a *running* state after having resumed another coroutine. When coroutines reach the end of the execution token for which they were initially created, they are then *dead* and can no longer be resumed.

There is coroutine-local state that is globally accessible within a coroutine. Coroutines initially inherit the coroutine-local state's value when they are created. Note that there is a global coroutine-local state outside of any coroutines that is used for initializing the coroutine-local states of new coroutines created outside of any coroutine.

An example of coroutines in action, in this case generating the Fibonacci sequence, is:

```
zscript-coroutine import

: fibonacci-coroutine ( -- coroutine )
  [:
    drop
    0 1 { x y }
    x suspend drop
    y suspend drop
    begin
      x y +
      y to x
      to y
      y suspend drop
    again
  ;] make-coroutine
;

: run-test ( -- )
  fibonacci-coroutine { co }
  25 0 ?do 0 co resume . loop
;
```

Executing `run-test` outputs:

```
0 1 1 2 3 5 8 13 21 34 55 89 144 233 377 610 987 1597 2584 4181 6765 10946 17711 28657 46368  ok
```

Another example of coroutines in action is:

```
zscript-coroutine import

: run-test
  [:
    .
    [:
      .
      -1 suspend .
      -2 suspend .
      -3 suspend .
      -4
    ;] make-coroutine { co }
    256 co resume .
    -256 suspend .
    257 co resume .
    -257 suspend .
    258 co resume .
    -258 suspend .
    -259
  ;] make-coroutine { co }
  0 co resume .
  1 co resume .
  2 co resume .
  3 co resume .
  4 co resume .
;
```

Executing `run-test` outputs:

```
0 256 -1 -256 1 257 -2 -257 2 258 -3 -258 3 -259 coroutine is dead
```

## `zscript-coroutine` words

### `make-coroutine`
( xt -- coroutine )

Create a coroutine with an execution token. Note that this coroutine will inherit the current coroutine-local state.

### `suspend`
( x -- x' )

Suspend the current coroutine, passing *x* to that which resumed it, and then returning *x'* passed in next time it is resumed.

### `resume`
( x coroutine -- x' )

Resume a coroutine, passing in *x*, and then returning *x'* passed in next time it is suspended.

### `current-coroutine`
( -- coroutine )

Get the current coroutine.

### `coroutine-state@`
( coroutine -- state )

Get the state of a coroutine.

### `coroutine-local@`
( -- x )

Get the current coroutine-local state. Note that this can be called outside of a coroutine, as there is a global not-in-coroutine state that is used for initializing the coroutine-local states of new coroutines.

### `coroutine-local!`
( x -- )

Set the current coroutine-local state. Note that this can be called outside of a coroutine, as there is a global not-in-coroutine state that is used for initializing the coroutine-local states of new coroutines.

### `suspended`
( -- state )

Suspended coroutine state.

### `running`
( -- state )

Running coroutine state.

### `dead`
( -- dead )

Dead coroutine state.

### `x-not-in-coroutine`
( -- )

Not in coroutine exception.

### `x-dead-coroutine`
( -- )

Attempted to resume a dead coroutine exception.

### `x-running-coroutine`
( -- )

Attempted to resume a running coroutine exception.