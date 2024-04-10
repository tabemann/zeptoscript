# zeptoscript Object-Orientation

zeptoscript optionally supports object-orientation. It is not traditional object-orientation in that there is no concept of inheritance, and methods are not declared as parts of particular classes. Rather, methods exist independent of any particular class, and any particular class can implement any set of methods. On the other hand, members are tied to individual classes, and cannot be used to access any other class.

Accesses to objects are safe in that any call to a method on an object that is not implemented by that object's class and any call to a member accessor on an object whose class does not match the accessor will result in an exception being raised.

Unlike records, objects in zeptoscript are not syntactic sugar for cell sequences. Also unlike records, classes in zeptoscript may have constructor methods, named `new`, which are automatically called when the objects are constructed. Note that unlike in some languages, objects do not have destructors or finalizers, so if such behavior is desired, adding a destructor or finalizer method, which may be named `destroy`, is necessary.

Creating a class with `begin-class` ( "class-name" -- ), followed by member and method definitions, and completed with `end-class` ( -- ), creates a single instantiation word, named `make-` followed by the name of the class specified. This word takes whatever arguments one wishes to pass onto the class's constructor, which are accompanied by the newly-constructed object, and returns the newly-constructed object.

Creating a member with `member:` ( "member-name" -- ) within a class definition creates two accessor words, consisting of the member name followed by `@`, for the getter`, and `!`, for the setter, which have the signatures ( *object* -- *x* ) and ( *x* *object* -- ) respectively. Note that these words must be defined before they may be referenced.

Implementing a method with `:method` ( "method-name" -- ) followed by its code and completed with `;` ( -- ) within a class definition implements the already-declared method with the specified name (which may include `::`) for that class. Note that the method must be declared ahead of time. When the method is called, the object for which it was called remains on the stack, so it can be used by the method. (Make sure to drop it off the stack.)

Here is a simple example of object-orientation in use:

```
private-module

zscript import
zscript-oo import

\ Declare our methods
method foo ( self -- )
method bar ( self -- )
method baz ( self -- )

\ Define one class which implements foo and bar
begin-class foobar

  \ Define the members of foobar
  member: foobar-x
  member: foobar-y

  \ Implement the methods of foobar

  \ Implement new, foobar's constructor
  :method new { x y self -- }
    x self foobar-x!
    y self foobar-y!
  ;

  \ Implement foo
  :method foo { self -- }
    ." foobar-x: " self foobar-x@ .
  ;

  \ Implement bar
  :method bar { self -- }
    ." foobar-y: " self foobar-y@ .
  ;

end-class

\ Define another class which implements bar and baz
begin-class barbaz

  \ Define the members of barbaz
  member: barbaz-y
  member: barbaz-z

  \ Implement the methods of barbaz

  \ Implement new, barbaz's constructor
  :method new { y z self -- }
    y self barbaz-y!
    z self barbaz-z!
  ;

  \ Implement bar
  :method bar { self -- }
    ." barbaz-y: " self barbaz-y@ .
  ;

  \ Implement baz
  :method baz { self -- }
    ." barbaz-z: " self barbaz-z@ .
  ;

end-class
```

With these definitions in place, one will get the following:

```
0 1 make-foobar foo foobar-x: 0  ok
0 1 make-foobar bar foobar-y: 1  ok
0 1 make-foobar baz unimplemented method
1 2 make-barbaz foo unimplemented method
1 2 make-barbaz bar barbaz-y: 1  ok
1 2 make-barbaz baz barbaz-z: 2  ok
``

## `zscript-oo` Words

### `method`
( "name" -- )

Declare a method with a given name. This method is not tied to any given class, and should be declared outside of a class definition. The method is called with an object of a class which implements it on the top of the stack, and that object will be passed, along with any other arguments, on the top of the stack to the implementation of the method for that object's class. If the method is called on an object whose class does not implement the method `x-unimplemented-method` will be raised.

### `begin-class`
( "name" -- )

Begin the definition of a class with a given name. Note that this class will be instantiated with `make-`*name*, which is defined within the context of the class (so the class can construct an instance of itself). Any extra values provided will be passed to the class's `new` method if there is one, along with the newly-minted instance of the class on the top of the stack; afterwards the new object will be returned.

### `end-class`
( -- )

Finish the definition of a class.

### `member:`
( "name" -- )

Define a member with a given name in the context of the definition of a class to which it will belong. Note that the member will be get and set with two methods *name*`@` ( *object* -- *x* ), its getter, and *name*`!` ( *x* *object* --- ), its setter. These accessors may only be used to access the class for which they are defined; otherwise `x-member-not-for-class` will be raised.

### `:method`
( "name" -- )

Implement a method with a given name in the context of the definition of a class to which it will belong. The method must already be declared with `method`. If a name of a word that is not a method is provided `x-not-a-method` will be raised. The method implementation will be finished with `;`.

### `x-member-not-for-class`
( -- )

Member is accessed on an object of a class to which it does not belong exception.

### `x-unimplemented-method`
( -- )

Method is called on an object of a class for which it is not implemented exception.

### `x-not-a-method`
( -- )

Method is attempted to be implemented for a word which is not a method exception.
