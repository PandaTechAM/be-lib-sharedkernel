namespace SharedKernel.Maintenance;

public enum MaintenanceMode
{
   Disabled = 0,
   EnabledForClients = 1,
   EnabledForAll = 2
}

//This is for local cache entity to poll the maintenance mode from distributed cache
//This should be removed then L1 + L2 cache is implemented in hybrid cache

// This is a local cache entity to hold the maintenance mode in memory
// This should be removed then L1 + L2 cache is implemented in hybrid cache
// thread-safe local snapshot