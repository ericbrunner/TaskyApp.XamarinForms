using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TaskyApp.Contracts;
using TaskyApp.Models;

namespace TaskyApp.Services
{
    public class TodosDataStore : IDataStore<Todo>
    {
        private readonly HttpClient _httpClient;
        private List<Todo> _memTodos = new List<Todo>();
        public TodosDataStore()
        {
            _httpClient = new HttpClient()
            {
                BaseAddress = new Uri("https://jsonplaceholder.typicode.com")
            };
        }
        public Task<bool> AddItemAsync(Todo item)
        {
            _memTodos.Add(item);

            return Task.FromResult(true);
        }

        public Task<bool> UpdateItemAsync(Todo item)
        {
            var updateItem = _memTodos.FirstOrDefault(i => i.Id == item.Id);

            if (updateItem != null)
            {
                updateItem.Completed = item.Completed;
                updateItem.Title = item.Title;
            }

            return Task.FromResult(true);
        }

        public Task<bool> DeleteItemAsync(long id)
        {
            var deleteItem = _memTodos.FirstOrDefault(i => i.Id == id);

            if (deleteItem != null)
            {
                _memTodos.Remove(deleteItem);
            }

            return Task.FromResult(true);
        }

        public Task<Todo> GetItemAsync(long id)
        {
            var lookupItem = _memTodos.FirstOrDefault(i => i.Id == id);

            return Task.FromResult(lookupItem);
        }

        public async Task<IEnumerable<Todo>> GetItemsAsync(bool forceRefresh = false)
        {
            try
            {
                using HttpResponseMessage response = await _httpClient.GetAsync("todos");

                response.EnsureSuccessStatusCode();

                var stream = await response.Content.ReadAsStreamAsync();
                var todos = await System.Text.Json.JsonSerializer.DeserializeAsync<List<Todo>>(stream);

                if (todos == null) return _memTodos;

                if (todos.Any())
                {
                    var newIds = todos.Select(t => t.Id);
                    var removedItems = _memTodos.RemoveAll(t => newIds.Contains(t.Id));

                    Debug.WriteLine($"{DateTime.Now:O}-{nameof(TodosDataStore)}.{nameof(GetItemsAsync)} Removed Items: {removedItems}");
                    
                    _memTodos.AddRange(todos);
                    
                    Debug.WriteLine($"{DateTime.Now:O}-{nameof(TodosDataStore)}.{nameof(GetItemsAsync)} Added Items: {todos.Count}");

                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{DateTime.Now:O}-{nameof(TodosDataStore)}.{nameof(GetItemsAsync)} Exception: {e.Message}");
            }
            
            return _memTodos;
        }
    }
}