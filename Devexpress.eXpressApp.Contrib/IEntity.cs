using System;
using DevExpress.Data.Filtering;
using DevExpress.Xpo;
using DevExpress.Xpo.Metadata;

namespace Devexpress.eXpressApp.Contrib
{
    public interface IEntity
    {
        Guid Oid { get; set; }
        object This { get; }
        bool Loading { get; }
        XPClassInfo ClassInfo { get; }
        Session Session { get; }
        bool IsLoading { get; }
        bool IsDeleted { get; }
        void Save();
        void Delete();
        void Reload();
        void SetMemberValue(string propertyName, object newValue);
        object GetMemberValue(string propertyName);
        object Evaluate(CriteriaOperator expression);
        bool Fit(CriteriaOperator condition);
        object Evaluate(string expression);
        bool Fit(string condition);
        object EvaluateAlias(string memberName);
        event ObjectChangeEventHandler Changed;
        void AfterConstruction();
        bool Equals(object obj);
        int GetHashCode();
        string ToString();
    }
}