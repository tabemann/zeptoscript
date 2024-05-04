# Queues

zeptoscript has support for simple FIFO queues of unlimited size. These queues are made use of by the zeptoscript multitasker and channel implementations.

## `zscript-queue` words

### `make-queue`
( -- queue )

Create a new, empty queue.

### `queue-empty?`
( queue -- empty? )

Get whether a queue is empty.

### `enqueue`
( element queue -- )

Add an element to the end of a queue.

### `dequeue`
( queue -- element success? )

Remove an element from the start of a queue and return the element and `true` for *success?* if the queue was not empty, else return 0 for the element and `false` for *success?*.

### `peek-queue`
( queue -- element success? )

Read an element from the start of the queue without dequeueing it and return the element and `true` for *success?* if the queue was not empty, else return 0 for the element and `false` for *success?*.

### `queue-size`
( queue -- size )

Get the number of elements in a queue.
