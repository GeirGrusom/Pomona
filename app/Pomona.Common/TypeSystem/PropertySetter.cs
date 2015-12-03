﻿using System;

namespace Pomona.Common.TypeSystem
{
    public class PropertySetter
    {
        private readonly Action<object, object, IContainer> del;


        public PropertySetter(Action<object, object, IContainer> del)
        {
            if (del == null)
                throw new ArgumentNullException(nameof(del));
            this.del = del;
        }


        public void Invoke(object target, object value, IContainer container)
        {
            container = container ?? new NoContainer();
            this.del(target, value, container);
        }

        public static implicit operator PropertySetter(Action<object, object, IContainer> del)
        {
            if (del == null)
                return null;
            return new PropertySetter(del);
        }

        public static explicit operator Action<object, object, IContainer>(PropertySetter propertySetter)
        {
            if (propertySetter == null)
                throw new ArgumentNullException(nameof(propertySetter));
            return propertySetter.del;
        }
    }
}