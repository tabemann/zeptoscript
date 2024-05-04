\ Copyright (c) 2024 Travis Bemann
\ 
\ Permission is hereby granted, free of charge, to any person obtaining a copy
\ of this software and associated documentation files (the "Software"), to deal
\ in the Software without restriction, including without limitation the rights
\ to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
\ copies of the Software, and to permit persons to whom the Software is
\ furnished to do so, subject to the following conditions:
\ 
\ The above copyright notice and this permission notice shall be included in
\ all copies or substantial portions of the Software.
\ 
\ THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
\ IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
\ FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
\ AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
\ LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
\ OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
\ SOFTWARE.

begin-module action

  \ zeptoscript must be initialized with ZSCRIPT::INIT-ZSCRIPT (whether since
  \ the last boot for if ZSCRIPT::INIT-ZSCRIPT was executed while compiling to
  \ flash before a prior reboot) before this can be used. Otherwise Bad Things
  \ will happen.

  \ Action is already in schedule exception
  : x-already-in-schedule ( -- ) ." action already in schedule" cr ;
  
  \ Action is not in a schedule exception
  : x-not-in-schedule ( -- ) ." action not in a schedule" cr ;

  \ Schedule is already running exception
  : x-schedule-already-running ( -- ) ." schedule is already running" cr ;

  \ Operation already set exception
  : x-operation-set ( -- ) ." operation already set" cr ;

  begin-module action-internal
    
    \ The current schedule
    global current-schedule
    
    \ The current action
    global current-action

    \ Make the error console available
    1 0 foreign forth::with-error-console with-error-console

    \ Display red
    0 0 foreign forth::display-red display-red

    \ Display normal
    0 0 foreign forth::display-normal display-normal

    \ Ring a bell
    0 0 foreign forth::bel bel

    \ Schedule records
    begin-record schedule

      \ Is the schedule running
      item: schedule-running?
      
      \ The next action to execute
      item: schedule-next
      
    end-record

    \ Action records
    begin-record action

      \ The action's schedule
      item: action-schedule
      
      \ The previous action
      item: action-prev

      \ The next action
      item: action-next

      \ The resume xt
      item: action-resume-xt

      \ The receive xt
      item: action-recv-xt

      \ The send xt
      item: action-send-xt
      
      \ The action's wait systick start
      item: action-systick-start

      \ The action's wait systick delay
      item: action-systick-delay

      \ The action's message source
      item: action-msg-src

      \ The action's message destination
      item: action-msg-dest
      
      \ The action's message data
      item: action-msg-data

    end-record

    \ No operation
    -2 constant no-operation

    \ No delay
    -1 constant no-delay

    \ No message
    -1 constant no-msg

    \ Find a message for the current action
    : find-msg { action -- msg-action | 0 }
      action action-schedule@ { schedule }
      schedule schedule-next@ dup { last-action }
      dup 0= if drop 0 exit then
      begin
        dup action-msg-dest@ action = if exit then
        action-next@
        dup last-action =
      until
      drop 0
    ;

    \ Clear sending message
    : clear-send-msg { src-action -- }
      no-operation src-action action-systick-delay!
      0 src-action action-msg-dest!
      src-action action-send-xt@ src-action action-resume-xt!
      0 src-action action-send-xt!
    ;

    \ Fail sending message
    : fail-send-msg { src-action -- }
      no-operation src-action action-systick-delay!
      0 src-action action-msg-dest!
      0 src-action action-send-xt!
    ;

    \ Clear send timeout
    : clear-send-timeout { src-action -- }
      no-operation src-action action-systick-delay!
      0 src-action action-msg-dest!
      0 src-action action-send-xt!
      0 src-action action-resume-xt!
    ;

    \ Clear receiving message
    : clear-recv-msg { dest-action -- }
      no-operation dest-action action-systick-delay!
      0 dest-action action-msg-src!
      0 dest-action action-resume-xt!
    ;

    \ Fail all sends to an action in a schedule
    : fail-all-send-msg { dest-action -- }
      dest-action action-schedule@ { schedule }
      schedule schedule-next@ dup { last-action }
      dup 0= if drop exit then
      begin
        dup action-msg-dest@ dest-action = if dup fail-send-msg then
        action-next@
        dup last-action =
      until
      drop
    ;

    \ Advance to the next action
    : advance-action { action -- }
      action action-schedule@ { schedule }
      action schedule schedule-next@ = if
        action action-next@ schedule schedule-next!
      then
    ;

    \ Get whether an action is the only action in a schedule
    : only-in-schedule? ( action -- flag )
      dup action-next@ =
    ;

    \ Set no delay for the current action
    : no-action-delay ( -- )
      no-delay current-action@ action-systick-delay!
    ;

    \ Validate the state of the current action
    : validate-current-action ( -- )
      current-action@ action-systick-delay@ no-operation =
      averts x-operation-set
    ;
    
  end-module> import

  \ Make a schedule
  : make-schedule ( -- schedule )
    make-schedule { schedule }
    false schedule schedule-running?!
    0 schedule schedule-next!
    schedule
  ;

  \ Make an action
  : make-action { init-xt -- action }
    make-action { action }
    0 action action-schedule!
    init-xt action action-resume-xt!
    no-operation action action-systick-start!
    no-operation action action-systick-delay!
    0 action action-recv-xt!
    0 action action-send-xt!
    0 action action-msg-src!
    0 action action-msg-dest!
    0 action action-msg-data!
    action
  ;

  \ Add an action to a schedule
  : add-action { schedule action -- }
    action action-schedule@ 0= averts x-already-in-schedule
    schedule action action-schedule!
    schedule schedule-next@ if
      schedule schedule-next@ { next-action }
      next-action action-prev@ { prev-action }
      action next-action action-prev!
      next-action action action-next!
      prev-action action action-prev!
      action prev-action action-next!
      action schedule schedule-next!
    else
      action schedule schedule-next!
      action action action-prev!
      action action action-next!
    then
  ;

  \ Remove an action from a schedule
  : remove-action { action -- }
    action action-schedule@ { schedule }
    schedule 0<> averts x-not-in-schedule
    action fail-all-send-msg
    action only-in-schedule? if
      0 schedule schedule-next!
    else
      action schedule schedule-next@ = if
        action action-next@ schedule schedule-next!
      then
      action action-next@ { next-action }
      action action-prev@ { prev-action }
      next-action prev-action action-next!
      prev-action next-action action-prev!
    then
    0 action action-prev!
    0 action action-next!
    0 action action-schedule!
  ;

  \ Send a message to an action with an option to handle failure; send-xt has
  \ the signature ( -- ) and fail-xt has the signature ( -- )
  : send-action-fail { send-xt fail-xt data dest-action -- }
    validate-current-action
    no-action-delay
    current-action@ { src-action }
    current-schedule@ { schedule }
    src-action action-schedule@ schedule = if
      dest-action action-schedule@ schedule = if
        dest-action action-msg-src@ no-msg = if
          data dest-action action-msg-data!
          src-action dest-action action-msg-src!
          send-xt src-action action-resume-xt!
        else
          dest-action src-action action-msg-dest!
          data src-action action-msg-data!
          fail-xt src-action action-resume-xt!
          send-xt src-action action-send-xt!
        then
      else
        fail-xt src-action action-resume-xt!
      then
    then
  ;

  \ Send a message to an action while silently handling failure; resume-xt has
  \ the signature ( -- )
  : send-action { resume-xt data dest-action -- }
    resume-xt resume-xt data dest-action send-action-fail
  ;

  \ Send a message to an action with an option to handle failure and timeout;
  \ send-xt has the signature ( -- ) and fail-xt has the signature ( -- )
  : send-action-timeout { timeout-ticks send-xt fail-xt data dest-action -- }
    validate-current-action
    current-action@ { src-action }
    current-schedule@ { schedule }
    src-action action-schedule@ schedule = if
      dest-action action-schedule@ schedule = if
        dest-action action-msg-src@ no-msg = if
          data dest-action action-msg-data!
          src-action dest-action action-msg-src!
          send-xt src-action action-resume-xt!
          no-action-delay
        else
          timeout-ticks 0>= if
            timeout-ticks src-action action-systick-delay!
            systick-counter src-action action-systick-start!
          else
            0 src-action action-systick-delay!
            systick-counter timeout-ticks + src-action action-systick-start!
          then
          dest-action src-action action-msg-dest!
          data src-action action-msg-data!
          fail-xt src-action action-resume-xt!
          send-xt src-action action-send-xt!
        then
      else
        fail-xt src-action action-resume-xt!
      then
    then
  ;

  \ Receive a message for the current action; recv-xt has the signature
  \ ( data src-action -- )
  : recv-action { recv-xt -- }
    validate-current-action
    no-action-delay
    current-action@ { dest-action }
    dest-action action-schedule@ current-schedule@ = if
      dest-action find-msg ?dup if { src-action }
        src-action dest-action action-msg-src!
        src-action action-msg-data@ dest-action action-msg-data!
        src-action clear-send-msg
      else
        no-msg dest-action action-msg-src!
        0 dest-action action-msg-data!
      then
      recv-xt dest-action action-recv-xt!
    then
  ;

  \ Receive a message for the current action with a timeout; recv-xt has the
  \ signature ( data src-action -- ) and timeout-xt has the signature ( -- )
  : recv-action-timeout { timeout-ticks recv-xt timeout-xt -- }
    validate-current-action
    current-action@ { dest-action }
    dest-action action-schedule@ current-schedule@ = if
      dest-action find-msg ?dup if { src-action }
        src-action dest-action action-msg-src!
        src-action action-msg-data@ dest-action action-msg-data!
        src-action clear-send-msg
        0 dest-action action-resume-xt!
        no-action-delay
      else
        timeout-ticks 0>= if
          timeout-ticks dest-action action-systick-delay!
          systick-counter dest-action action-systick-start!
        else
          0 dest-action action-systick-delay!
          systick-counter timeout-ticks + dest-action action-systick-start!
        then
        no-msg dest-action action-msg-src!
        0 dest-action action-msg-data!
        timeout-xt dest-action action-resume-xt!
      then
      recv-xt dest-action action-recv-xt!
    then
  ;

  \ Delay the current action
  : delay-action { ticks resume-xt -- }
    validate-current-action
    current-action@ { action }
    ticks 0>= if
      ticks action action-systick-delay!
      systick-counter action action-systick-start!
    else
      0 action action-systick-delay!
      systick-counter ticks + action action-systick-start!
    then
    resume-xt action action-resume-xt!
  ;

  \ Delay the current action from a given time
  : delay-action-from-time { systick-start systick-delay resume-xt -- }
    validate-current-action
    current-action@ { action }
    systick-delay 0>= if
      systick-delay action action-systick-delay!
      systick-start action action-systick-start!
    else
      0 action action-systick-delay!
      systick-start systick-delay + action action-systick-start!
    then
    resume-xt action action-resume-xt!
  ;

  \ Yield the current action
  : yield-action { resume-xt -- }
    validate-current-action
    no-action-delay
    resume-xt current-action@ action-resume-xt!
  ;

  continue-module action-internal
    
    \ Execute a word and handle exceptions while continuing
    : execute-handle ( xt -- )
      try ?dup if
        [: display-red try drop display-normal bel ;] unsafe::xt>integral
        with-error-console
        current-action@ remove-action
      then
    ;
    
  end-module

  \ Run schedule
  : run-schedule { schedule -- }
    schedule schedule-running?@ triggers x-schedule-already-running
    schedule current-schedule!
    true schedule schedule-running?!
    begin schedule schedule-running?@ while
      schedule schedule-next@ { next-action }
      next-action if
        next-action action-msg-src@ ?dup if
          dup no-msg = if
            drop
            next-action action-systick-delay@ { systick-delay }
            systick-delay 0>=
            systick-counter next-action action-systick-start@ -
            systick-delay >= and if
              next-action current-action!
              next-action advance-action
              next-action action-resume-xt@
              next-action clear-recv-msg
              execute-handle
            else
              next-action advance-action
            then
          else
            next-action action-msg-data@
            swap
            next-action advance-action
            next-action action-recv-xt@
            next-action clear-recv-msg
            next-action current-action!
            execute-handle
          then
        else
          next-action action-send-xt@ 0= if
            next-action action-resume-xt@ if
              next-action action-systick-delay@ { systick-delay }
              systick-delay 0<
              systick-counter next-action action-systick-start@ -
              systick-delay >= or if
                next-action current-action!
                next-action advance-action
                next-action action-resume-xt@
                no-operation next-action action-systick-delay!
                0 next-action action-resume-xt!
                execute-handle
              else
                advance-action
              then
            else
              next-action advance-action
              next-action remove-action
            then
          else
            next-action action-resume-xt@ if
              next-action action-systick-delay@ { systick-delay }
              systick-delay 0>=
              systick-counter next-action action-systick-start@ -
              systick-delay >= and if
                next-action current-action!
                next-action advance-action
                next-action action-resume-xt@
                next-action clear-send-timeout
                execute-handle
              else
                advance-action
              then
            else
              advance-action
            then
          then
        then
      else
        pause
      then
    repeat
  ;

  \ Get the schedule of an action
  : action-schedule@ ( action -- schedule ) action-schedule@ ;

  \ Get whether an action is in a schedule
  : in-schedule? ( action -- flag ) action-schedule@ 0<> ;

  \ Stop a running schedule cleanly
  : stop-schedule ( schedule -- ) false swap schedule-running?! ;

  \ Get the current schedule
  : current-schedule ( -- schedule ) current-schedule@ ;

  \ Get the current action
  : current-action ( -- action ) current-action@ ;
  
end-module