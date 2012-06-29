using DevExpress.ExpressApp.DC;
using DevExpress.Xpo;

namespace Devexpress.eXpressApp.Contrib
{
    public class DCEntitity : DCBaseObject, IEntity
    {
        public DCEntitity(Session session)
            : base(session)
        {

        }

        public void RefreshView(string propertyName)
        {
            TriggerObjectChanged(new ObjectChangeEventArgs(Session, this, ObjectChangeReason.PropertyChanged));
        }
    }
}