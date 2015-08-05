# CConst

This project provides compile-time support for pure (const) C# functions. A pure function has no side-effects.

Annotating a method with the Const attribute declares it as a pure function. The diagnostics in this project will then report on any side-effects.

* **CConst1**: A pure function may not contain an assignment to a non-local variable.
    ```c#
    using FonsDijkstra.CConst;
    
    class C
    {
        int i;
        
        [Const]
        int F()
        {
            i = 3; // reports diagnostic CConst1
            return 0;
        }
    }
    ```
* **CConst2**: A pure function may not call a non-pure function.
    ```c#
    using FonsDijkstra.CConst;
    
    class C
    {
        [Const]
        int F()
        {
            G(); // reports diagnostic CConst2
            return 0;
        }

        void G()
        {
        }
    }
    ```
* **CConst51**: An override of a pure function must be a pure function itself.
    ```C#
    using FonsDijkstra.CConst;
    
    class A
    {
        [Const]
        public abstract bool F();
    }

    class B : A
    {
        public override bool F() // reports diagnostic CConst51
        {
            return false;
        }
    }
    ```
* **CConst52**: An interface implementation of a pure function must be a pure function itself.
    ```c#
    using FonsDijkstra.CConst;
    
    interface I
    {
        [Const]
        bool F();
    }

    class C : I
    {
        public bool F() // reports diagnostic CConst52
        {
            return false;
        }
    }
    ```

