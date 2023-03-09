using Moq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace TestProject1
{
    [TestClass]
    public class UnitTest1
    {
        IMyContext _context;

        [TestInitialize]
        public void Init()
        {
            var dummyData = new List<Person>
            {
                new Person{Id=1, FirstName="Chris",LastName="Johnson"},
                new Person{Id=2, FirstName="Sally",LastName="Sue"},
            };
            var mockPersonSet = EntityFrameworkMockHelper.GetMockDbSet<Person>(dummyData);
            var mockContext = new Mock<IMyContext>();
            mockContext.Setup(x => x.People).Returns(mockPersonSet.Object);
            _context = mockContext.Object;
        }

        [TestMethod]
        public void TestMethod1()
        {
            var a = MyDb.GetCPeople(_context).Result;
            Assert.IsTrue(a.Any(), "a Records Exist");
            Assert.IsTrue(a.Count() == 1, "a Only One Record");
            Assert.IsTrue(a.First().FirstName == "Chris", "a Record is the one with 'Chris' as first name");
            var b = MyDb.GetCPeople(_context).Result;
            Assert.IsTrue(b.Any(),"b Records Exist");
            Assert.IsTrue(b.Count() == 1, "b Only One Record");
            Assert.IsTrue(b.First().FirstName == "Chris", "b Record is the one with 'Chris' as first name");
        }

        [TestMethod]
        public void TestMethod2()
        {
            var cstr = "Data Source=(LocalDb)\\MSSQLLocalDB;Initial Catalog=Contacts;Integrated Security=SSPI";
            var context = new MyRealContext(cstr);
            var a = MyDb.GetCPeople(context).Result;
            Assert.IsTrue(a.Any(), "a Records Exist");
            Assert.IsTrue(a.Count() == 1, "a Only One Record");
            Assert.IsTrue(a.First().FirstName == "Chris", "a Record is the one with 'Chris' as first name");
            var b = MyDb.GetCPeople(context).Result;
            Assert.IsTrue(b.Any(), "b Records Exist");
            Assert.IsTrue(b.Count() == 1, "b Only One Record");
            Assert.IsTrue(b.First().FirstName == "Chris", "b Record is the one with 'Chris' as first name");
        }
    }

    public class MyDb
    {
        public static async Task<IEnumerable<Person>> GetCPeople(IMyContext context)
        {
            var regex = new Regex("^[Cc]");
            var dbpeople = (await context.People.ToListAsync().ConfigureAwait(false));
            var peopleWhosNameStartsWithC =
                dbpeople
                .Where(p => regex.IsMatch(p.FirstName));
            return peopleWhosNameStartsWithC;
        }
    }

    public class MyRealContext : DbContext, IMyContext
    {
        public MyRealContext(string connectionString):base(connectionString)
        {

        }

        public DbSet<Person> People { get; set; }
    }

    public interface IMyContext
    {
        DbSet<Person> People { get; set; }
    }

    public class Person
    {
        [Key]
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }


    public class EntityFrameworkMockHelper
    {
        public static Mock<DbSet<T>> GetMockDbSet<T>(List<T> sourceList) where T : class
        {
            var queryable = sourceList.AsQueryable();

            var dbSet = new Mock<DbSet<T>>();

            dbSet.As<IDbAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator())
                .Returns(new DbAsyncEnumerator<T>(queryable.GetEnumerator()));


            dbSet.As<IQueryable<T>>()
                .Setup(m => m.Provider)
                .Returns(new DbAsyncQueryProvider<T>(queryable.Provider));

            dbSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            dbSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            dbSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());
            dbSet.Setup(d => d.Add(It.IsAny<T>())).Callback<T>((s) => sourceList.Add(s));
            dbSet.Setup(d => d.Remove(It.IsAny<T>())).Callback<T>((s) => sourceList.Remove(s));
            dbSet.Setup(m => m.Include(It.IsAny<string>())).Returns(dbSet.Object);

            return dbSet;
        }
    }

    internal class DbAsyncQueryProvider<TEntity> : IDbAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal DbAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new DbAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new DbAsyncEnumerable<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public Task<object> ExecuteAsync(Expression expression, CancellationToken cancellationToken)
        {
            return Task.FromResult(Execute(expression));
        }

        public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            return Task.FromResult(Execute<TResult>(expression));
        }
    }

    internal class DbAsyncEnumerable<T> : EnumerableQuery<T>, IDbAsyncEnumerable<T>, IQueryable<T>
    {
        public DbAsyncEnumerable(IEnumerable<T> enumerable)
            : base(enumerable)
        { }

        public DbAsyncEnumerable(Expression expression)
            : base(expression)
        { }

        public IDbAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new DbAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        IDbAsyncEnumerator IDbAsyncEnumerable.GetAsyncEnumerator()
        {
            return GetAsyncEnumerator();
        }

        IQueryProvider IQueryable.Provider
        {
            get { return new DbAsyncQueryProvider<T>(this); }
        }
    }

    internal class DbAsyncEnumerator<T> : IDbAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public DbAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public void Dispose()
        {
            _inner.Dispose();
        }

        public Task<bool> MoveNextAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_inner.MoveNext());
        }

        public T Current
        {
            get { return _inner.Current; }
        }

        object IDbAsyncEnumerator.Current
        {
            get { return Current; }
        }
    }
}