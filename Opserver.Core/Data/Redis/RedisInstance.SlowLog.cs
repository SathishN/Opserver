﻿using System.Collections.Generic;
using System.Linq;
using BookSleeve;

namespace StackExchange.Opserver.Data.Redis
{
    public partial class RedisInstance
    {
        private const string ConfigParamSlowLogThreshold = "slowlog-log-slower-than";
        private const string ConfigParamSlowLogMaxLength = "slowlog-max-len";

        /// <summary>
        /// Is Slow Log enabled on this instance, determined by checking the slow-log-slower-than config value
        /// </summary>
        /// <remarks>
        /// For setup instructions call <see cref="SetSlowLogThreshold"/> and <see cref="SetSlowLogMaxLength"/> or see: http://redis.io/commands/slowlog
        /// </remarks>
        public bool IsSlowLogEnabled
        {
            get
            {
                string configVal;
                int numVal;
                return Config.HasData()
                       && Config.Data.TryGetValue(ConfigParamSlowLogThreshold, out configVal)
                       && int.TryParse(configVal, out numVal)
                       && numVal > 0;
            }
        }

        private Cache<List<CommandTrace>> _slowLog;
        public Cache<List<CommandTrace>> SlowLog
        {
            get
            {
                return _slowLog ?? (_slowLog = new Cache<List<CommandTrace>>
                {
                    CacheForSeconds = 60,
                    UpdateCache = GetFromRedis("SlowLog", rc => rc.Wait(rc.Server.GetSlowCommands(200)).ToList())
                });
            }
        }

        /// <summary>
        /// Sets the slow log threshold in milliseconds, note: 0 logs EVERY command, null or negative disables logging.
        /// </summary>
        /// <param name="minMilliseconds">Minimum milliseconds before a command is logged, null or 0 means disabled</param>
        public void SetSlowLogThreshold(int? minMilliseconds)
        {
            var value = minMilliseconds > 0 ? (minMilliseconds*1000).ToString() : null;
            SetConfigValue(ConfigParamSlowLogThreshold, value);
        }

        /// <summary>
        /// Sets the max retention of the slow log
        /// </summary>
        /// <param name="numItems">Max number of items to keep in the slow log</param>
        public void SetSlowLogMaxLength(int numItems)
        {
            SetConfigValue(ConfigParamSlowLogMaxLength, numItems.ToString());
        }

        /// <summary>
        /// Clears the SlowLog for this redis instance
        /// </summary>
        /// <remarks>
        /// </remarks>
        public void ClearSlowLog()
        {
            Connection.Server.ResetSlowCommands();
        }
    }
}