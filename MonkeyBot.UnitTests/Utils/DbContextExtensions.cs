using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace MonkeyBot.UnitTests.Utils
{
    internal static class DbContextExtensions
    {
        internal static Mock<DbSet<TEnt>> SetDbSetData<TEnt, TCon>(this Mock<TCon> dbMock, IList<TEnt> list, Expression<Func<TCon, DbSet<TEnt>>> expression) where TEnt : class
                                                                                                                  where TCon: DbContext
        {
            var clonedList = list.ToList();
            var mockDbSet = clonedList.AsQueryable().BuildMockDbSet();

            dbMock.Setup(expression).Returns(mockDbSet.Object);

            return mockDbSet;
        }
    }
}
