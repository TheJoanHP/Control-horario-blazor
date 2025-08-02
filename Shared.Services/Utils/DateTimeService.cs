namespace Shared.Services.Utils
{
    public static class DateTimeService
    {
        /// <summary>
        /// Convierte una fecha UTC a la zona horaria especificada
        /// </summary>
        public static DateTime ConvertToTimeZone(DateTime utcDateTime, string timeZoneId)
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZone);
        }

        /// <summary>
        /// Convierte una fecha de zona horaria específica a UTC
        /// </summary>
        public static DateTime ConvertToUtc(DateTime localDateTime, string timeZoneId)
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZone);
        }

        /// <summary>
        /// Obtiene el inicio del día (00:00:00)
        /// </summary>
        public static DateTime GetStartOfDay(DateTime date)
        {
            return date.Date;
        }

        /// <summary>
        /// Obtiene el final del día (23:59:59.999)
        /// </summary>
        public static DateTime GetEndOfDay(DateTime date)
        {
            return date.Date.AddDays(1).AddTicks(-1);
        }

        /// <summary>
        /// Obtiene el inicio de la semana (lunes)
        /// </summary>
        public static DateTime GetStartOfWeek(DateTime date, DayOfWeek startOfWeek = DayOfWeek.Monday)
        {
            int diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

        /// <summary>
        /// Obtiene el final de la semana (domingo)
        /// </summary>
        public static DateTime GetEndOfWeek(DateTime date, DayOfWeek startOfWeek = DayOfWeek.Monday)
        {
            return GetStartOfWeek(date, startOfWeek).AddDays(6).AddDays(1).AddTicks(-1);
        }

        /// <summary>
        /// Obtiene el inicio del mes
        /// </summary>
        public static DateTime GetStartOfMonth(DateTime date)
        {
            return new DateTime(date.Year, date.Month, 1);
        }

        /// <summary>
        /// Obtiene el final del mes
        /// </summary>
        public static DateTime GetEndOfMonth(DateTime date)
        {
            return GetStartOfMonth(date).AddMonths(1).AddTicks(-1);
        }

        /// <summary>
        /// Calcula los días laborables entre dos fechas
        /// </summary>
        public static int GetWorkingDays(DateTime startDate, DateTime endDate)
        {
            int workingDays = 0;
            var current = startDate.Date;
            
            while (current <= endDate.Date)
            {
                if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                {
                    workingDays++;
                }
                current = current.AddDays(1);
            }
            
            return workingDays;
        }

        /// <summary>
        /// Verifica si una fecha es día laborable
        /// </summary>
        public static bool IsWorkingDay(DateTime date)
        {
            return date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday;
        }

        /// <summary>
        /// Obtiene la siguiente fecha laborable
        /// </summary>
        public static DateTime GetNextWorkingDay(DateTime date)
        {
            var nextDay = date.AddDays(1);
            while (!IsWorkingDay(nextDay))
            {
                nextDay = nextDay.AddDays(1);
            }
            return nextDay;
        }

        /// <summary>
        /// Calcula la diferencia en horas laborables entre dos fechas
        /// </summary>
        public static TimeSpan GetWorkingHours(DateTime startDateTime, DateTime endDateTime, TimeSpan workStartTime, TimeSpan workEndTime)
        {
            var totalHours = TimeSpan.Zero;
            var current = startDateTime.Date;
            
            while (current <= endDateTime.Date)
            {
                if (IsWorkingDay(current))
                {
                    var dayStart = current.Add(workStartTime);
                    var dayEnd = current.Add(workEndTime);
                    
                    var effectiveStart = startDateTime > dayStart ? startDateTime : dayStart;
                    var effectiveEnd = endDateTime < dayEnd ? endDateTime : dayEnd;
                    
                    if (effectiveStart < effectiveEnd)
                    {
                        totalHours = totalHours.Add(effectiveEnd - effectiveStart);
                    }
                }
                current = current.AddDays(1);
            }
            
            return totalHours;
        }

        /// <summary>
        /// Formatea una duración en formato legible
        /// </summary>
        public static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalDays >= 1)
            {
                return $"{duration.Days}d {duration.Hours}h {duration.Minutes}m";
            }
            else if (duration.TotalHours >= 1)
            {
                return $"{duration.Hours}h {duration.Minutes}m";
            }
            else
            {
                return $"{duration.Minutes}m";
            }
        }

        /// <summary>
        /// Redondea el tiempo al minuto más cercano
        /// </summary>
        public static DateTime RoundToNearestMinute(DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 
                               dateTime.Hour, dateTime.Minute, 0);
        }

        /// <summary>
        /// Redondea el tiempo al intervalo especificado (en minutos)
        /// </summary>
        public static DateTime RoundToInterval(DateTime dateTime, int intervalMinutes)
        {
            var totalMinutes = dateTime.Hour * 60 + dateTime.Minute;
            var roundedMinutes = (totalMinutes / intervalMinutes) * intervalMinutes;
            
            return dateTime.Date.AddMinutes(roundedMinutes);
        }
    }
}