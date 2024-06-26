# Unbounded channels

Unbounded channels are used by tasks to send each other messages. They consist of queues with an unlimited number of elements.

Sending on an unbounded channel always returns immediately. The new element will be enqueued, and if a task is waiting to receive an element it will be woken up and will then receive the element; otherwise the element will remain queued for the next task to receive it. Note that as sending on unbounded channels never blocks, the only way to guarantee that the receiving task will actually receive the element is to call `zscript-task::yield` after sending on the  ubnounded channels.

If there are greater than zero elements in a queue and a task attempts to receive from an unbonded channel, the next element will be dequeued; otherwise the receiving task will block until another task sends on the queue.

## `zscript-uchan` words

### `make-uchan`
( -- uchannel )

Create an unbounded channel.

### `send`
( message uchannel -- )

Send a message to an unbounded channel. If there are blocked receiving tasks, wake up one receiving task. Note that this always returns immediately.

### `recv`
( uchannel -- message )

Receive a message from a unbounded channel. If the unbounded channel is empty, block until a sending task provides a message to receive.

### `recv-non-block`
( uchannel -- message success? )

Receive a message from a unbounded channel in a non-blocking fashion. If the unbounded channel is not empty, return the message and `true`, else return `0` and `false`.

### `peek`
( uchannel -- message )

Peek a message from a unbounded channel, i.e. read it without dequeuing it. If the unbounded channel is empty, block until a sending task provides a message to peek.

### `peek-non-block`
( uchannel -- message success? )

Peek a message from a unbounded channel, i.e. read it without dequeueing it, in a non-blocking fashion. If the unbounded channel is not empty, return the message and `true`, else return `0` and `false`.
