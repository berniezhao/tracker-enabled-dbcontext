﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TrackerEnabledDbContext.Common;
using TrackerEnabledDbContext.Common.Configuration;
using TrackerEnabledDbContext.Common.EventArgs;
using TrackerEnabledDbContext.Common.Interfaces;
using TrackerEnabledDbContext.Common.Models;

namespace TrackerEnabledDbContext
{
    public class TrackerContext : DbContext, ITrackerContext
    {
        private CoreTracker _coreTracker;

        public TrackerContext()
        {
            InitializeCoreTracker();
        }

        public TrackerContext(string connectinString)
            : base(connectinString)
        {
            InitializeCoreTracker();
        }

        public TrackerContext(DbConnection dbconnection, bool contextOwnsConnection)
            : base(dbconnection, contextOwnsConnection)
        {
            InitializeCoreTracker();
        }

        private void InitializeCoreTracker()
        {
            _coreTracker = new CoreTracker(this);
            _coreTracker.OnAuditLogGenerated += RaiseOnAuditLogGenerated;
        }

        public event EventHandler<AuditLogGeneratedEventArgs> OnAuditLogGenerated;

        public DbSet<AuditLog> AuditLog { get; set; }

        public DbSet<AuditLogDetail> LogDetails { get; set; }

        /// <summary>
        ///     This method saves the model changes to the database.
        ///     If the tracker for an entity is active, it will also put the old values in tracking table.
        ///     Always use this method instead of SaveChanges() whenever possible.
        /// </summary>
        /// <param name="userName">Username of the logged in identity</param>
        /// <returns>Returns the number of objects written to the underlying database.</returns>
        public virtual int SaveChanges(object userName)
        {
            if (!GlobalTrackingConfig.Enabled) return base.SaveChanges();

            _coreTracker.AuditChanges(userName);

            IEnumerable<DbEntityEntry> addedEntries = _coreTracker.GetAdditions();
            // Call the original SaveChanges(), which will save both the changes made and the audit records...Note that added entry auditing is still remaining.
            int result = base.SaveChanges();
            //By now., we have got the primary keys of added entries of added entiries because of the call to savechanges.

            _coreTracker.AuditAdditions(userName, addedEntries);

            //save changes to audit of added entries
            base.SaveChanges();
            return result;
        }

        /// <summary>
        ///     This method saves the model changes to the database.
        ///     If the tracker for an entity is active, it will also put the old values in tracking table.
        /// </summary>
        /// <returns>Returns the number of objects written to the underlying database.</returns>
        public override int SaveChanges()
        {
            if (!GlobalTrackingConfig.Enabled) return base.SaveChanges();
            //var user = Thread.CurrentPrincipal?.Identity?.Name ?? "Anonymous"; 
            return SaveChanges(null);
        }

        /// <summary>
        ///     Get all logs for the given model type
        /// </summary>
        /// <typeparam name="TEntity">Type of domain model</typeparam>
        /// <returns></returns>
        public IQueryable<AuditLog> GetLogs<TEntity>()
        {
            return _coreTracker.GetLogs<TEntity>();
        }

        /// <summary>
        ///     Get all logs for the given entity name
        /// </summary>
        /// <param name="entityName">full name of entity</param>
        /// <returns></returns>
        public IQueryable<AuditLog> GetLogs(string entityName)
        {
            return _coreTracker.GetLogs(entityName);
        }

        /// <summary>
        ///     Get all logs for the given model type for a specific record
        /// </summary>
        /// <typeparam name="TEntity">Type of domain model</typeparam>
        /// <param name="primaryKey">primary key of record</param>
        /// <returns></returns>
        public IQueryable<AuditLog> GetLogs<TEntity>(object primaryKey)
        {
            return _coreTracker.GetLogs<TEntity>( primaryKey);
        }

        /// <summary>
        ///     Get all logs for the given entity name for a specific record
        /// </summary>
        /// <param name="entityName">full name of entity</param>
        /// <param name="primaryKey">primary key of record</param>
        /// <returns></returns>
        public IQueryable<AuditLog> GetLogs(string entityName, object primaryKey)
        {
            return _coreTracker.GetLogs(entityName, primaryKey);
        }

        #region -- Async --

        /// <summary>
        ///     Asynchronously saves all changes made in this context to the underlying database.
        ///     If the tracker for an entity is active, it will also put the old values in tracking table.
        /// </summary>
        /// <param name="userName">Username of the logged in identity</param>
        /// <param name="cancellationToken">
        ///     A System.Threading.CancellationToken to observe while waiting for the task
        ///     to complete.
        /// </param>
        /// <returns>Returns the number of objects written to the underlying database.</returns>
        public virtual async Task<int> SaveChangesAsync(object userName, CancellationToken cancellationToken)
        {
            if (!GlobalTrackingConfig.Enabled) return await base.SaveChangesAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                cancellationToken.ThrowIfCancellationRequested();

            _coreTracker.AuditChanges(userName);

            IEnumerable<DbEntityEntry> addedEntries = _coreTracker.GetAdditions();

            // Call the original SaveChanges(), which will save both the changes made and the audit records...Note that added entry auditing is still remaining.
            int result = await base.SaveChangesAsync(cancellationToken);

            //By now., we have got the primary keys of added entries of added entiries because of the call to savechanges.
            _coreTracker.AuditAdditions(userName, addedEntries);

            //save changes to audit of added entries
            await base.SaveChangesAsync(cancellationToken);

            return result;
        }

        /// <summary>
        ///     Asynchronously saves all changes made in this context to the underlying database.
        ///     If the tracker for an entity is active, it will also put the old values in tracking table.
        ///     Always use this method instead of SaveChangesAsync() whenever possible.
        /// </summary>
        /// <returns>Returns the number of objects written to the underlying database.</returns>
        public virtual async Task<int> SaveChangesAsync(int userId)
        {
            if (!GlobalTrackingConfig.Enabled) return await base.SaveChangesAsync(CancellationToken.None);

            return await SaveChangesAsync(userId, CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously saves all changes made in this context to the underlying database.
        ///     If the tracker for an entity is active, it will also put the old values in tracking table.
        ///     Always use this method instead of SaveChangesAsync() whenever possible.
        /// </summary>
        /// <returns>Returns the number of objects written to the underlying database.</returns>
        public virtual async Task<int> SaveChangesAsync(string userName)
        {
            if (!GlobalTrackingConfig.Enabled) return await base.SaveChangesAsync(CancellationToken.None);

            return await SaveChangesAsync(userName, CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously saves all changes made in this context to the underlying database.
        ///     If the tracker for an entity is active, it will also put the old values in tracking table with null UserName.
        /// </summary>
        /// <returns>
        ///     A task that represents the asynchronous save operation.  The task result
        ///     contains the number of objects written to the underlying database.
        /// </returns>
        public override async Task<int> SaveChangesAsync()
        {
            if (!GlobalTrackingConfig.Enabled) return await base.SaveChangesAsync(CancellationToken.None);

            return await SaveChangesAsync(null, CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously saves all changes made in this context to the underlying database.
        ///     If the tracker for an entity is active, it will also put the old values in tracking table with null UserName.
        /// </summary>
        /// <param name="cancellationToken">
        ///     A System.Threading.CancellationToken to observe while waiting for the task
        ///     to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous save operation.  The task result
        ///     contains the number of objects written to the underlying database.
        /// </returns>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            if (!GlobalTrackingConfig.Enabled) return await base.SaveChangesAsync(cancellationToken);

            return await SaveChangesAsync(null, cancellationToken);
        }

        #endregion

        private void RaiseOnAuditLogGenerated(object sender, AuditLogGeneratedEventArgs e)
        {
            OnAuditLogGenerated?.Invoke(sender, e);
        }

        public new void Dispose()
        {
            ReleaseEventHandlers();
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void ReleaseEventHandlers()
        {
            _coreTracker.OnAuditLogGenerated -= OnAuditLogGenerated;
        }
    }
}