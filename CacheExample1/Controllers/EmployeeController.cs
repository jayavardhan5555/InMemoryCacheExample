﻿using CacheExample1.Models;
using CacheExample1.Models.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace CacheExample1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private static readonly SemaphoreSlim GetUsersSemaphore = new SemaphoreSlim(1, 1);
        private const string employeeListCacheKey = "employeeList";
        private readonly IDataRepository<Employee> _dataRepository;
        private IMemoryCache _cache;
        private ILogger<EmployeeController> _logger;

        public EmployeeController(IDataRepository<Employee> dataRepository, IMemoryCache cache,
        ILogger<EmployeeController> logger)
        {
            _dataRepository = dataRepository;
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        // GET: api/Employee
        [HttpGet]
        public  IActionResult Get()
        {
            _logger.Log(LogLevel.Information, "Trying to fetch the list of employees from cache.");

            try
            {
                 GetUsersSemaphore.Wait();

                if (_cache.TryGetValue(employeeListCacheKey, out IEnumerable<Employee> employees))
                {
                    _logger.Log(LogLevel.Information, "Employee list found in cache.");
                }
                else
                {
                    _logger.Log(LogLevel.Information, "Employee list not found in cache. Fetching from database.");
                    employees = _dataRepository.GetAll();

                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromSeconds(60))
                        .SetAbsoluteExpiration(TimeSpan.FromSeconds(3600))
                        .SetPriority(CacheItemPriority.Normal)
                        .SetSize(1024);

                    _cache.Set(employeeListCacheKey, employees, cacheEntryOptions);
                }
                return Ok(employees);
            }
            finally
            {
                GetUsersSemaphore.Release();
                
            }
           
           
        }
        // GET: api/Employee/5
        [HttpGet("{id}", Name = "Get")]
        public IActionResult Get(long id)
        {
            Employee employee = _dataRepository.Get(id);
            if (employee == null)
            {
                return NotFound("The Employee record couldn't be found.");
            }
            return Ok(employee);
        }
        // POST: api/Employee
        [HttpPost]
        public IActionResult Post([FromBody] Employee employee)
        {
            if (employee == null)
            {
                return BadRequest("Employee is null.");
            }
            _dataRepository.Add(employee);
            return CreatedAtRoute(
                  "Get",
                  new { Id = employee.EmployeeId },
                  employee);
        }
        // PUT: api/Employee/5
        [HttpPut("{id}")]
        public IActionResult Put(long id, [FromBody] Employee employee)
        {
            if (employee == null)
            {
                return BadRequest("Employee is null.");
            }
            Employee employeeToUpdate = _dataRepository.Get(id);
            if (employeeToUpdate == null)
            {
                return NotFound("The Employee record couldn't be found.");
            }
            _dataRepository.Update(employeeToUpdate, employee);
            return NoContent();
        }
        // DELETE: api/Employee/5
        [HttpDelete("{id}")]
        public IActionResult Delete(long id)
        {
            Employee employee = _dataRepository.Get(id);
            if (employee == null)
            {
                return NotFound("The Employee record couldn't be found.");
            }
            _dataRepository.Delete(employee);
            return NoContent();
        }
    }
}