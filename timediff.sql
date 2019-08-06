SELECT
(logtimeint / 10000 - time / 10000) * 3600 + (logtimeint % 10000 / 100 - time % 10000 / 100) * 60
+ (logtimeint % 100 - time % 100) as DataAgeInSeconds,
(logtimeint / 10000 - sendtime / 10000) * 3600 + (logtimeint % 10000 / 100 - sendtime % 10000 / 100) * 60
+ (logtimeint % 100 - sendtime % 100) as ReceivingPingInSeconds,
RowId, logtime, id, count,
optionbase, fullcode, classcode,
logtimeint, time, sendtime,
json FROM Ticks where classcode = 'SPBFUT' and (logtimeint > time and time > 0) --только тики за текущий день и хотя бы с одной последней сделкой
and DataAgeInSeconds > 120 --подозрительные тики, у которых последняя сделка давно
and fullcode not in ('BR-10.19', 'BR-11.19', 'BR-12.19', 'Si-12.19', 'Si-3.20', 'Si-6.20', 'ED-12.19') --неликвидные фьючерсы, где последняя сделка была очень давно
and optionbase not in ('MOEX', 'SBRF', 'U500')
and (sendtime not between 140000 and 140500)
and (sendtime not between 184500 and 190000)