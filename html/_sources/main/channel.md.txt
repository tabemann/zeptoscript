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

### `recv`
( channel -- message )

Receive a message from a channel. If the channel is empty, block until a sending task provides a message to receive. Also, if there are blocked sending tasks, wake up one sending task.