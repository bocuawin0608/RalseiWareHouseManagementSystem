using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RalseiWarehouse.Models;
using RalseiWarehouse.Services;
using UserAccount = RalseiWarehouse.Models.User;
namespace RalseiWarehouse.ViewModels;
/// <summary>Coordinates administrator-only role and user maintenance.</summary>
public partial class AdministrationViewModel(IWarehouseService service) : ViewModelBase
{
 public ObservableCollection<Role> Roles { get; }=[]; public ObservableCollection<UserAccount> Users { get; }=[]; [ObservableProperty] private Role? selectedRole; [ObservableProperty] private UserAccount? selectedUser; [ObservableProperty] private string newPassword="";
 /// <summary>Loads roles and users.</summary>
 [RelayCommand] public Task LoadAsync()=>ExecuteAsync(RefreshAsync);
 /// <summary>Starts a role.</summary>
 [RelayCommand] private void NewRole(){SelectedRole=new();Message="Fill in the fields and click Save.";}
 /// <summary>Saves the role.</summary>
 [RelayCommand] private Task SaveRoleAsync()=>WriteAsync(()=>service.SaveRoleAsync(SelectedRole??throw Select()));
 /// <summary>Deletes the role.</summary>
 [RelayCommand] private Task DeleteRoleAsync()=>WriteAsync(()=>service.DeleteRoleAsync((SelectedRole??throw Select()).RoleId));
 /// <summary>Starts a user.</summary>
 [RelayCommand] private void NewUser(){SelectedUser=new();NewPassword="";Message="Fill in the fields and click Save.";}
 /// <summary>Saves the user.</summary>
 [RelayCommand] private Task SaveUserAsync()=>WriteAsync(()=>service.SaveUserAsync(SelectedUser??throw Select(),NewPassword));
 /// <summary>Deletes the user.</summary>
 [RelayCommand] private Task DeleteUserAsync()=>WriteAsync(()=>service.DeleteUserAsync((SelectedUser??throw Select()).UserId));
 /// <summary>Refreshes administrator lists.</summary>
 private async Task RefreshAsync(){Replace(Roles,await service.GetRolesAsync());Replace(Users,await service.GetUsersAsync());}
 /// <summary>Runs a write and refresh.</summary>
 private Task WriteAsync(Func<Task> operation)=>ExecuteAsync(async()=>{await operation();await RefreshAsync();Message="Saved successfully.";});
 /// <summary>Creates a selection error.</summary>
 private static InvalidOperationException Select()=>new("Select a record first.");
}
