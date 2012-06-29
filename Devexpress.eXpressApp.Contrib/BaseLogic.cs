using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using DevExpress.Xpo;

namespace Devexpress.eXpressApp.Contrib
{
    public class BaseLogic : NotBrowsableObject
    {
    }

    public abstract class BaseLogic<T> : BaseLogic where T : class
    {
        #region Initialization

        /// <summary>
        /// Use the Init function to register onproperty changed events with this domain
        /// logic class
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public abstract void Init();

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public void AfterConstruction(T target, IObjectSpace objectSpace)
        {
            //assign the weakreference
            Entity = new WeakReference(target);

            //let users do their stuff to
            AfterConstructionInternal(target, objectSpace);
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public void OnLoaded(T target, IObjectSpace objectSpace)
        {
            //recreate the weakreference
            Entity = new WeakReference(target);

            //let users do their stuff to
            OnLoadedInternal(target, objectSpace);
        }

        #endregion

        public virtual void OnLoadedInternal(T target, IObjectSpace objectSpace)
        {
        }

        public virtual void AfterConstructionInternal(T target, IObjectSpace objectSpace)
        {
        }

        public void RegisterMultiplePropertyChanged(Expression<Func<T, object[]>> properties, Func<T, bool> onlyWhen, Action<T, ChangeEventArgs<object>> onChanged)
        {
            var binaryExpression = properties.Body as NewArrayExpression;

            if (binaryExpression == null)
                return;

            foreach (var expression in binaryExpression.Expressions)
            {
                MemberExpression memberExpression = null;

                if (expression is UnaryExpression)
                {
                    memberExpression = (expression as UnaryExpression).Operand as MemberExpression;
                }
                else if (expression is MemberExpression)
                {
                    memberExpression = expression as MemberExpression;
                }

                if (memberExpression == null)
                    continue;

                RegisterPropertyChanged(memberExpression, onlyWhen, onChanged);
            }
        }

        public void RegisterPropertyChanged<TProp>(Expression<Func<T, TProp>> property, Action<T, ChangeEventArgs<TProp>> onChanged) where TProp : class
        {
            RegisterPropertyChanged(property, x => true, onChanged);
        }

        public void RegisterPropertyChanged<TProp>(MemberExpression memberExpression, Func<T, bool> onlyWhen, Action<T, ChangeEventArgs<TProp>> onChanged) where TProp : class
        {
            if (memberExpression == null)
                return;

            //extract the property name
            var memberName = memberExpression.Member.Name;

            //checks if weakreference was created and alive
            if (Entity == null || Entity.Target == null || !Entity.IsAlive)
                return;

            //if you would let your domain components derive from another class, this will not work
            var dcEntitity = Entity.Target as DCEntitity;

            //checks if the target of weakreference is a DCEntity
            if (dcEntitity == null)
                return;

            //listen to the entity change event
            dcEntitity.Changed += (sender, args) =>
            {
                //_exclusivePropChange is used to prevent an infinite loop of propertychanged events,
                //this is possible when a user assigns a new value to the property in the onChange Action
                if (_exclusivePropChange)
                    return;

                //check for registered propertyname
                if (args.PropertyName == null || args.PropertyName != memberName || args.Reason != ObjectChangeReason.PropertyChanged)
                    return;

                //check if the conditions to trigger onChange were met
                if (!onlyWhen(sender as T))
                    return;

                try
                {
                    SecuritySystem.Demand(new ObjectAccessPermission(typeof(T), ObjectAccess.Read, ObjectAccessModifier.Allow));
                }
                catch (SecurityException)
                {
                    return;
                }

                if (args.OldValue == args.NewValue)
                    return;

                //execute onChanged action
                _exclusivePropChange = true;

                var changeEventArgs = new ChangeEventArgs<TProp>(args, args.PropertyName);
                onChanged(sender as T, changeEventArgs);

                if (changeEventArgs.RefreshAfterChange)
                    System.Threading.Tasks.Task.Factory.StartNew(() => dcEntitity.RefreshView(args.PropertyName),
                                                                 CancellationToken.None, TaskCreationOptions.None,
                                                                 TaskScheduler.FromCurrentSynchronizationContext());

                _exclusivePropChange = false;

            };
        }

        public void RegisterPropertyChanged<TProp>(Expression<Func<T, TProp>> property, Func<T, bool> onlyWhen, Action<T, ChangeEventArgs<TProp>> onChanged) where TProp : class
        {
            //parse memberexpression to check if only a property was provided
            var memberExpression = property.Body as MemberExpression;

            RegisterPropertyChanged(memberExpression, onlyWhen, onChanged);
        }

        #region Fields

        private WeakReference _entity;
        private bool _exclusivePropChange;
        private WeakReference Entity
        {
            get { return _entity; }
            set
            {
                _entity = value;
                Init();
            }
        }

        #endregion

        #region Helper Classes

        public class ChangeEventArgs<TProp> : NotBrowsableObject where TProp : class
        {
            public T Entity { get; private set; }
            public TProp OldValue { get; private set; }
            public TProp NewValue { get; private set; }
            public string PropertyName { get; set; }
            public bool RefreshAfterChange { get; set; }

            public ChangeEventArgs(ObjectChangeEventArgs objectChangeEventArgs, string propertyName)
            {
                PropertyName = propertyName;
                Entity = objectChangeEventArgs.Object as T;

                NewValue = objectChangeEventArgs.NewValue as TProp;
                OldValue = objectChangeEventArgs.OldValue as TProp;

                RefreshAfterChange = false;
            }
        }

        #endregion

        #region Interfaces

        public interface IAfterConstruction
        {
            void AfterConstruction(T target, IObjectSpace objectSpace);
        }

        public interface IOnDeleted
        {
            void OnDeleted(T target, IObjectSpace objectSpace);
        }

        public interface IOnDeleting
        {
            void OnDeleting(T target, IObjectSpace objectSpace);
        }

        public interface IOnSaving
        {
            void OnSaving(T target, IObjectSpace objectSpace);
        }

        public interface IOnSaved
        {
            void OnSaved(T target, IObjectSpace objectSpace);
        }

        public interface IOnLoaded
        {
            void OnLoaded(T target, IObjectSpace objectSpace);
        }

        #endregion
    }
}