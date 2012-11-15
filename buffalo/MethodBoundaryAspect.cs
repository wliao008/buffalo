﻿using Buffalo.Interfaces;

namespace Buffalo
{
    public abstract class MethodBoundaryAspect : System.Attribute, IMethodBoundaryAspect
    {
        public MethodBoundaryAspect()
        {
            this.AttributeExclude = false;
        }

        public MethodBoundaryAspect(bool attributeExclude = false)
        {
            this.AttributeExclude = attributeExclude;
        }

        public bool AttributeExclude { get; set; }

        public virtual void OnBefore(MethodArgs args) { }

        public virtual void OnAfter(MethodArgs args) { }

        public virtual void OnSuccess(MethodArgs args) { }

        public virtual void OnException(MethodArgs args) { }
    }
}
