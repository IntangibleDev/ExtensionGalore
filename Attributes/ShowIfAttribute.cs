using System;
using UnityEngine;

namespace ExtensionGalore.Attributes
{
    public enum DrawType
    {
        /// <summary> Greys out the field, and doesn't hide it. </summary>
        GreyOut,
        /// <summary> Hides the field completely, so that it isn't visible. </summary>
        DontDraw,
        /// <summary> Hides the field completely, so that it isn't visible, but keeps the height. </summary>
        DontDrawKeepHeight,
    }


    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class ShowIfAttribute : PropertyAttribute
    {
        public readonly DrawType drawType;
        public readonly string expression;

        public string greyOutToolTip = null;

        /// <summary> </summary>
        /// <param name="expression">If the expression is false, the attribute will be hidden from the inspector.</param>
        public ShowIfAttribute(string expression, DrawType drawType = DrawType.GreyOut)
        {
            this.expression = expression;
            this.drawType = drawType;
        }
    }
}
