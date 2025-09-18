using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using HRApp.Models;
using HRApp.Repositories;

public interface IEmployeeRepository : IRepository<Employee>
{
    // Custom methods if needed, but base GetAll handles filters
    IEnumerable<object> GetAll(Func<object, bool> value, string includeProperties);
}