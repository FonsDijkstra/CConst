using System;

namespace CConst
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class ConstAttribute : Attribute
    {
    }
}
