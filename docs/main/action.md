# Actions

zeptoscript does not support multitasking itself (non-zeptoscript tasks can execute, but only one zeptoscript environment can exist, in only one task), so in its place zeptoscript has *actions*, like zeptoforth actions, to provide asynchronous execution with a single task.

Actions belong to *schedules*, one of which may execute in a given task at a time. Within a schedule actions execute in a cooperative, round-robin fashion. They may also send messages to and receive messages from each other. An action may only send a single message to or receive a single message from one another at a time. Messaging is strictly synchronous; an action will not continue executing after sending a message until the message is received, sending the message times out, or sending the message otherwise fails (e.g. the target action terminates).

Actions all share the same stack as one another; persistent state belonging to an action is achieved through binding values (e.g. a record or object) to the states as closures or through globals. Also note that operations set for actions only occur after the handler for the current operation state completes, and that `x-operation-set` will be raised if the user attempts to set another operation after having already set an operation for the current state.

Messages allocated on the heap are not copied when being set from one action to another. This avoids the expense of having to copy data from one buffer to another and the complexity of having to manage space to store messages to send or receive. In this way actions in zeptoscript differ from actions in zeptoforth. This also means that messages can be modified after they are sent or received, which in most cases will be undesirable; to avoid this use `zscript::duplicate` or `zscript-list::duplicate-list` if this would turn out to be an issue.

## `zscript-action` words

### `make-schedule`
( -- schedule )

Create an empty schedule.

### `make-action`
( init-xt -- action )

Create an action with an initial state of *init-xt*.

### `add-action`
( schedule action -- )

Add *action* to *schedule*.

### `remove-action`
( action -- )

Remove *action* from its current schedule.

### `send-action-fail`
( send-xt fail-xt data dest-action -- ) send-xt: ( -- ) fail-xt: ( -- )

Set the current action to send a message *data* to *dest-action*, and call *send-xt* on success or *fail-xt* on failure.

### `send-action`
( resume-xt data dest-action -- ) resume-xt: ( -- )

Set the current action to send a message *data* to *dest-action*, and call *resume-xt* afterwards regardless of success or failure.

### `send-action-timeout`
( timeout-ticks send-xt fail-xt data dest-action -- ) send-xt: ( -- ) fail-xt: ( -- )

Set the current action to send a message *data* to *dest-action* with a timeout of *timeout-ticks*, and call *send-xt* on success or *fail-xt* on failure.

### `recv-action`
( recv-xt -- ) recv-xt: ( data src-action -- )

Set the current action to receive a message, and call *recv-xt* with received *data* from *src-action* on message receipt.

### `recv-action-timeout`
( timeout-ticks recv-xt timeout-xt -- ) recv-xt: ( data src-action -- ) timeout-xt: ( -- )

Set the current action to receive a message with a timeout of *timeout-ticks*, and call *recv-xt* with received *data* from *src-action* on message receipt, or *timeout-xt* on timeout.

### `delay-action`
( ticks resume-xt -- ) resume-xt: ( -- )

Set the current action to wait *ticks* and then call *resume-xt*.

### `delay-action-from-time`
( systick-start systick-delay resume-xt -- ) resume-xt: ( -- )

Set the current action to wait *systick-delay* ticks from *systick-start* ticks and th en call *resume-xt*.

### `yield-action`
( resume-xt -- ) resume-xt: ( -- )

Set the current action to give up control and then call *resume-xt* when it regains control.

### `run-schedule`
( schedule -- )

Run the schedule and its actions.

### `stop-schedule`
( schedule -- )

Set the schedule to cleanly stop.

### `action-schedule@`
( action -- schedule )

Get the schedule of *action*.

### `in-schedule?`
( action -- flag )

Get whether an action is in a schedule.

### `current-schedule`
( -- schedule )

Get the current schedule.

### `current-action`
( -- action )

Get the current action.

### `x-already-in-schedule`
( -- )

Action already in schedule exception.

### `x-not-in-schedule`
( -- )

Action not in schedule exception.

### `x-schedule-already-running`
( -- )

Schedule already running exception.

### `x-operation-set`
( -- )

Action already has an operation set exception.
