# Channels

Channels are used by tasks to send each other messages. They consist of queues with fixed maximum numbers of elements.

If there are fewer elements than the maximum in a queue and a task attempts to send on a channel, the new element will be enqueued, and if a task is waiting to receive an element it will be woken up and will then receive the element; otherwise the sending task will block until another task receives from the queue to free up space for the new element.

If there are greater than zero elements in a queue and a task attempts to receive from a channel, the next element will be dequeued, and if a task is waiting to send an element it will be woken up and will then send the element; otherwise the receiving task will block until another task sends on the queue.

## `zscript-chan` words

### `make-chan`
( size -- channel )

Create a channel with a maximum number of elements of *size*.

### `send`
( message channel -- )

Send a message to a channel. If the channel is full, block until a receiving task frees up space for the message. Also, if there are blocked receiving tasks, wake up one receiving task.

### `send-non-block`
( message channel -- success? ).

Send a message to a channel in a non-blocking fashion, If the channel has room to enqueue the message, return `true`, else return `false`.

### `recv`
( channel -- message )

Receive a message from a channel. If the channel is empty, block until a sending task provides a message to receive. Also, if there are blocked sending tasks, wake up one sending task.

### `recv-non-block`
( channel -- message success? )

Receive a message from a channel in a non-blocking fashion. If the channel is not empty, return the message and `true`, else return `0` and `false`. Also, if there are blocked sending task, wake up one sending task.

### `peek`
( channel -- message )

Peek a message from a channel, i.e. read it without dequeuing it. If the channel is empty, block until a sending task provides a message to peek.

### `peek-non-block`
( channel -- message success? )

Peek a message from a channel, i.e. read it without dequeueing it, in a non-blocking fashion. If the channel is not empty, return the message and `true`, else return `0` and `false`.
