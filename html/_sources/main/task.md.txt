# Tasks

zeptoscript supports cooperative multitasking, distinct from zeptoforth's preemptive multitasking. zeptoscript tasks all share a single zeptoforth task and a single zeptoscript heap. zeptoscript tasks execute in a round-robin fashion without priorities. Through the use of saved states each task has practically separate data and return stacks, even though they share their parent zeptoforth task's data and return stack underneath it all.

## `zscript-task` words

### `spawn`
( task -- )

Spawn a task which will execute *task* when executed.

### `yield`
( -- )

Yield the currently running task, adding it to the schedule and executing the next task ready to be executed. Note that if there are no other tasks ready to run this will return immediately.

### `terminate`
( -- )

Execute the next ready task without rescheduling the current task; note that this returns if there is no next ready task.

### `fork`
( -- parent? )

Fork the current task into two tasks, returning `true` for the parent task and `false` for the child task.

### `start`
( -- )

Start executing scheduled tasks, if there are any. If there are scheduled tasks, this does not return.

### `wake`
( queue -- )

If there are any tasks in a queue, dequeue the next task from the queue and schedule it.

### `block`
( queue -- )

If there are any tasks ready to execute, enqueue the current task into the queue and execute the next ready task.

### `wait-delay`
( start-time delay -- )

Wait for *delay* ticks after *start-time* ticks.

### `ms` -- )
( ms -- )

Wait for *ms* milliseconds
