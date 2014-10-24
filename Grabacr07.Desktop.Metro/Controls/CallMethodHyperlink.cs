using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Livet.Behaviors;

namespace Grabacr07.Desktop.Metro.Controls
{
    /// <summary>
    /// Button-like Hyperlink
    /// </summary>
    public class CallMethodHyperlink : Hyperlink
    {
        static CallMethodHyperlink()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CallMethodHyperlink), new FrameworkPropertyMetadata(typeof(CallMethodHyperlink)));
        }

        private readonly MethodBinder binder = new MethodBinder();
        private readonly MethodBinderWithArgument binderWithArgument = new MethodBinderWithArgument();
        private bool hasParameter;

        #region MethodTarget 依存関係プロパティ

        public object MethodTarget
        {
            get { return this.GetValue(MethodTargetProperty); }
            set { this.SetValue(MethodTargetProperty, value); }
        }
        public static readonly DependencyProperty MethodTargetProperty =
            DependencyProperty.Register("MethodTarget", typeof(object), typeof(CallMethodHyperlink), new UIPropertyMetadata(null));

        #endregion

        #region MethodName 依存関係プロパティ

        public string MethodName
        {
            get { return (string)this.GetValue(MethodNameProperty); }
            set { this.SetValue(MethodNameProperty, value); }
        }
        public static readonly DependencyProperty MethodNameProperty =
            DependencyProperty.Register("MethodName", typeof(string), typeof(CallMethodHyperlink), new UIPropertyMetadata(null));

        #endregion

        #region MethodParameter 依存関係プロパティ

        public object MethodParameter
        {
            get { return this.GetValue(MethodParameterProperty); }
            set { this.SetValue(MethodParameterProperty, value); }
        }
        public static readonly DependencyProperty MethodParameterProperty =
            DependencyProperty.Register("MethodParameter", typeof(object), typeof(CallMethodHyperlink), new UIPropertyMetadata(null, MethodParameterPropertyChangedCallback));

        private static void MethodParameterPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (CallMethodHyperlink)d;
            source.hasParameter = true;
        }

        #endregion

        /// <summary>
        /// Button-like functionality for a Hyperlink element
        /// </summary>
        protected override void OnClick()
        {
            base.OnClick();

            if (string.IsNullOrEmpty(this.MethodName)) return;

            var target = this.MethodTarget ?? this.DataContext;
            if (target == null) return;

            if (this.hasParameter)
            {
                this.binderWithArgument.Invoke(target, this.MethodName, this.MethodParameter);
            }
            else
            {
                this.binder.Invoke(target, this.MethodName);
            }
        }
    }
}
