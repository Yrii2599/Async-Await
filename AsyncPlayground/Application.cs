using AsyncPlayground.Entities;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace AsyncPlayground
{
    internal class Application
    {
        private Dictionary<string, int> _cache = new() { ["x"] = 42 };
        private readonly ApplicationContext _context;

        public Application(ApplicationContext context)
        {
            _context = context;
        }

        public async Task Go()
        {
            Task1_HelloWorld();
            await Task2_ReturnTaskToAddEmployeeAsync();
            await Task3_Exception();
            await Task4_ReturnEmployees();
            await Task5_Disposal();
            await Task6_MultipleCalls();
            await Task7_1_GetFirstEmployeeNameAsync();
            await Task7_2_CorrectBlockingAsync();
            await Task8_FireAndForgetAsync();
            await Task9_GetCachedValueAsync();
            await Task10_Cancellation();
        }

        // Task 1. Unnecessary State Machine involved.
        private void Task1_HelloWorld()
        {
            Console.WriteLine("Hello World!");
        }

        // Task 2. Everything looks fine... or does it?
        private async Task<int> Task2_ReturnTaskToAddEmployeeAsync()
        {
            try
            {
                return await CreateEmployee();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        private async Task<int> CreateEmployee()
        {
            // Add a new employee to the database
            var employee = new Employee
            {
                Name = "John Doe",
                Department = "IT"
            };
            _context.Employees.Add(employee);
            return await _context.SaveChangesAsync();
        }

        // Task 3. We should see the exception message in the output
        private async Task Task3_Exception()
        {
            await CatchTheExceptionAsync();
        }

        private async Task AsyncVoidMethodThrowsException()
        {
            await Task.Delay(100);
            throw new Exception("Hmmm, something went wrong!");
        }

        public async Task CatchTheExceptionAsync()
        {
            try
            {
                await AsyncVoidMethodThrowsException();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        // Task 4. Return the list of employees
        private Task<List<Employee>> Task4_ReturnEmployees()
        {
            return ReturnEmployeesListAsync();
        }

        private Task<List<Employee>> ReturnEmployeesListAsync()
        {
            return _context.Employees.ToListAsync();
        }

        // Task 5. Avoid early disposal
        private async Task Task5_Disposal()
        {
            var result = await ReturnTaskToReadFileEarlyDisposeAsync();
            Console.WriteLine("Task5 " + result);
        }

        public async Task<string> ReturnTaskToReadFileEarlyDisposeAsync()
        {
            using (var reader = new StreamReader("config.json"))
            {
                return await reader.ReadToEndAsync();
            }
        }

        // Task 6. Optimize multiple calls that are not dependent on each other
        private async Task<List<Employee>> Task6_MultipleCalls()
        {
            var employeesFromDepartment1 = GetEmployeesFromDepartmentAsync("IT");
            var employeesFromDepartment2 = GetEmployeesFromDepartmentAsync("Financial");
            var employeesFromDepartment3 = GetEmployeesFromDepartmentAsync("BI");

            var result = new List<Employee>();
            var results = await Task.WhenAll(employeesFromDepartment1, employeesFromDepartment2, employeesFromDepartment3);

            return results.SelectMany(e => e).ToList();
        }

        private async Task<List<Employee>> GetEmployeesFromDepartmentAsync(string department)
        {
            return await _context.Employees.Where(e => e.Department == department).ToListAsync();
        }

        // Task 7.1. I just don't like AggregateExceptions
        private async Task<string> Task7_1_GetFirstEmployeeNameAsync()
        {
            return await GetFirstEmployeeNameAsync();
        }

        private async Task<string> GetFirstEmployeeNameAsync()
        {
            var emp = await _context.Employees.FirstOrDefaultAsync();
            return emp!.Name;
        }

        // Task 7.2. I just don't like AggregateExceptions
        private async Task Task7_2_CorrectBlockingAsync()
        {
            await LongImportantJobThatShouldBeAwaited();
        }

        private async Task LongImportantJobThatShouldBeAwaited()
        {
            await Task.Delay(5000);
        }

        // Task 8. Avoid Fire-and-Forget Without Logging or Handling
        private async Task Task8_FireAndForgetAsync()
        {
            try
            {
                await DoBackgroundWorkAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Task8_FireAndForget: {ex.Message}");
            }
        }

        private async Task DoBackgroundWorkAsync()
        {
            await Task.Delay(1000);
            Console.WriteLine("Background work completed!");
            throw new Exception("Background work failed!");
        }

        private static readonly Random _random = new Random();
        // Task 9: This method is being called really often. Try to optimize its memory consumption.
        public async Task<int> Task9_GetCachedValueAsync()
        {
            if (_cache.TryGetValue("x", out var value))
                return value;

            return await FetchValueAsync();
        }

        private async Task<int> FetchValueAsync()
        {
            await Task.Delay(100);
            return _random.Next();
        }

        // Task 10. Provide cancellation mechanism for the long-running task
        private async Task Task10_Cancellation()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var task = LongRunningTaskAsync(cancellationTokenSource.Token);

            await Task.Delay(3000).ContinueWith(_ => cancellationTokenSource.Cancel());

            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Task was canceled.");
            }
        }

        public async Task LongRunningTaskAsync(CancellationToken cancellationToken)
        {
            for (int i = 0; i < 100; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }

                //Simulate an async call that takes some time to complete
                await Task.Delay(1000);
            }
        }
    }
}
