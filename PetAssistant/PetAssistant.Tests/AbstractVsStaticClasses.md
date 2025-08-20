# Abstract Classes vs Static Classes in C#

## Abstract Classes vs Static Classes

### Abstract Class
- **Cannot be instantiated** directly (like static)
- **Can have both static AND instance members**
- **Designed for inheritance** - must be inherited by concrete classes
- **Can have constructors** (for derived classes to call)
- **Can have abstract methods** (no implementation) that derived classes must implement
- **Can have virtual methods** that derived classes can override

### Static Class
- **Cannot be instantiated** 
- **Only static members allowed**
- **Cannot be inherited** or inherit from others
- **No constructors allowed**
- **All methods must have implementations**

## Methods in Abstract Classes

```csharp
public abstract class ExampleAbstract
{
    // Static method - belongs to the class
    public static void StaticMethod() { }
    
    // Instance method - belongs to instances
    public void InstanceMethod() { }
    
    // Abstract method - no implementation, must be overridden
    public abstract void AbstractMethod();
    
    // Virtual method - has implementation, can be overridden
    public virtual void VirtualMethod() { }
}
```

## Instantiation Rules

Both abstract and static classes **cannot be instantiated directly**, but for different reasons:

### Static Class
- Cannot be instantiated **at all**
- No `new StaticClass()` allowed - compile error

### Abstract Class
- Cannot be instantiated **directly**
- But can be instantiated through **derived classes**
- `new AbstractClass()` - compile error
- `new ConcreteClass()` where `ConcreteClass : AbstractClass` - allowed

### Example
```csharp
public abstract class Animal
{
    public abstract void MakeSound();
}

public class Dog : Animal
{
    public override void MakeSound() => Console.WriteLine("Woof");
}

// This won't work:
// var animal = new Animal(); // Compile error

// This works:
var dog = new Dog(); // Creates instance of derived class
Animal animal = new Dog(); // Polymorphism - abstract reference to concrete object
```

So abstract classes are instantiated **indirectly** through inheritance, while static classes are never instantiated.

## Key Difference
**Abstract classes** support inheritance and polymorphism, while **static classes** are pure utility containers.