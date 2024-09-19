using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using InstrumentationAmplifier.Ad71248;
using InstrumentationAmplifier.Services;
using InstrumentationAmplifier.Utils;
using static InstrumentationAmplifier.Utils.CommonUtils;

namespace InstrumentationAmplifier.ViewModels;

public partial class MainViewModel
{
    private bool InitializeAd71248([NotNullWhen(false)] out string? error)
    {
        error = null;
        try
        {
            _ad71248.Initialize(PowerModes.FullPower);
            Thread.Sleep(6);

            _ad71248.SetChannel(0, AdcChannel0);
            _ad71248.SetChannel(1, AdcChannel1);
            _ad71248.SetConfiguration(0, AdcConfiguration0);
            _ad71248.SetConfiguration(1, AdcConfiguration1);
            _ad71248.SetFilter(0, AdcFilter0);
            _ad71248.SetFilter(1, AdcFilter1);
            Thread.Sleep(2);

            int id = _ad71248.GetId();
            if (id == 0) error = "Ошибка проверки подключения АЦП";
        }
        catch (Exception e)
        {
            error = $"Ошибка инициализации АЦП\n{e.ToShortString()}";
        }

        return error == null;
    }

    private async Task EnsureAd71248Initialized()
    {
        for (;;)
        {
            if (InitializeAd71248(out string? error)) break;

            await _dialogService.ShowMessage(error, "Попробовать снова");
        }
    }

    private Dictionary<byte, (double voltage, uint data)> ReadAdc(int times, CancellationToken ct = default) => ReadAdc(times, null, ct);

    private Dictionary<byte, (double voltage, uint data)> ReadAdc(int times, int[]? channelsToRead = null, CancellationToken ct = default)
    {
        if (channelsToRead != null)
        {
            _ad71248.SetChannel(0, AdcChannel0 with { Enable = channelsToRead.Contains(0) });
            _ad71248.SetChannel(1, AdcChannel1 with { Enable = channelsToRead.Contains(1) });
        }

        var dataSource = _ad71248.GetContinuousData(ct);
        using var dataEnumerator = dataSource.GetEnumerator();

        Dictionary<byte, List<(double voltage, uint data)>> channelsData = new();
        //foreach (var channel in Channels) measurements[channel.Number] = new(times);

        try
        {
            int enabledChannels = _ad71248.Channels.Count(r => r.Enable);
            for (int i = 0; i < times * enabledChannels; i++)
            {
                dataEnumerator.MoveNext();
                var data = dataEnumerator.Current;

                if (!channelsData.TryGetValue(data.Status.ActiveChannel, out var list))
                    list = channelsData[data.Status.ActiveChannel] = new(times);

                list.Add((data.Voltage, data.Data));
            }
        }
        finally
        {
            if (channelsToRead != null)
            {
                _ad71248.SetChannel(0, AdcChannel0);
                _ad71248.SetChannel(1, AdcChannel1);
            }
        }

        ct.ThrowIfCancellationRequested();
        Dictionary<byte, (double voltage, uint data)> results = new();

        //Txt = "";
        foreach (var channel in channelsData)
        {
            var list = channel.Value;
            //Console.WriteLine(string.Join(", ", list.Select(v => $"{v.voltage:0.00000}")));
            //if (list.Count == 0) throw new InvalidOperationException("No data");
            list.Sort();
            double median = GetMedianInSortedArray(list, e => e.voltage);
            double averageDeviation = list.Select(v => Math.Abs(median - v.voltage)).Average();
            var correctData = list.Where(v => Math.Abs(median - v.voltage) <= averageDeviation * 3).ToList();
            var averageVoltage = correctData.Average(v => v.voltage);
            uint averageData = (uint)correctData.Average(v => v.data);
            results[channel.Key] = (averageVoltage, averageData);
            /*Log($"{channel.Key} > " + string.Join(" | ", channel.Value.Select(v => $"{v:0.00000}")) +
                   " = " + results[channel.Key].ToString("0.00000"));*/
        }

        return results;
    }

    private Dictionary<byte, Pga> AdjustAdcGains(CancellationToken ct = default)
    {
        Dictionary<byte, Pga> channelsGain = new();
        int enabledChannels = _ad71248.Channels.Count(r => r.Enable);

        for (;;)
        {
            bool pgaChanged = false;
            Log("Gain adjust cycle start");

            using (var dataSource = _ad71248.GetContinuousData(ct))
            {
                using var dataEnumerator = dataSource.GetEnumerator();

                for (int i = 0; i < enabledChannels; i++)
                {
                    dataEnumerator.MoveNext();
                    var data = dataEnumerator.Current;

                    if (!channelsGain.TryGetValue(data.Status.ActiveChannel, out var pga))
                        pga = channelsGain[data.Status.ActiveChannel] = Pga.Pga1;

                    Pga nextPga = pga;
                    // If ADC saturated (all 1)
                    if (pga != Pga.Pga1 && data.Data == (1 << Ad71248Device.BitsCount) - 1)
                    {
                        nextPga = nextPga.Previous();
                    }
                    else if (pga != Pga.Pga128)
                    {
                        // TODO: max n step?
                        while (nextPga != Pga.Pga128 && data.Voltage < nextPga.Next().GetMaxVoltage())
                        {
                            nextPga = nextPga.Next();
                        }
                    }

                    if (pga != nextPga)
                    {
                        channelsGain[data.Status.ActiveChannel] = nextPga;
                        pgaChanged = true;

                        Log($"ADC gain ~: {data.Status.ActiveChannel} - {data.Voltage:0.#####}V | Gain {pga.Gain()} -> {nextPga.Gain()}");
                    }
                }
            }

            if (!pgaChanged) break;

            Log(JsonSerializer.Serialize(channelsGain));
            if (channelsGain.TryGetValue(0, out var gain0))
                _ad71248.SetConfiguration(0, AdcConfiguration0 with { Pga = gain0 });
            if (channelsGain.TryGetValue(1, out var gain1))
                _ad71248.SetConfiguration(1, AdcConfiguration1 with { Pga = gain1 });
        }

        return channelsGain;
    }

    private Dictionary<byte, (double voltage, uint data, byte gain)> ReadAdcWithGain(int times, CancellationToken ct = default) =>
        ReadAdcWithGain(times, null, ct);

    private Dictionary<byte, (double voltage, uint data, byte gain)> ReadAdcWithGain(
        int times, int[]? channelsToRead = null, CancellationToken ct = default)
    {
        if (channelsToRead != null)
        {
            _ad71248.SetChannel(0, AdcChannel0 with { Enable = channelsToRead.Contains(0) });
            _ad71248.SetChannel(1, AdcChannel1 with { Enable = channelsToRead.Contains(1) });
        }

        Dictionary<byte, List<(double voltage, uint data, byte gain)>> channelsData = new();
        int enabledChannels = _ad71248.Channels.Count(r => r.Enable);

        Dictionary<byte, Pga> gains;
        try
        {
            Log("Gain adjust start");
            gains = AdjustAdcGains(ct);
            Log("Gain adjust end: " + JsonSerializer.Serialize(gains));

            using var dataSource = _ad71248.GetContinuousData(ct);
            using var dataEnumerator = dataSource.GetEnumerator();

            for (int i = 0; i < times * enabledChannels; i++)
            {
                dataEnumerator.MoveNext();
                var data = dataEnumerator.Current;

                if (!channelsData.TryGetValue(data.Status.ActiveChannel, out var list))
                    list = channelsData[data.Status.ActiveChannel] = new(times);

                list.Add((data.Voltage, data.Data, gains[data.Status.ActiveChannel].Gain()));
            }

            Log("Adc end");
        }
        finally
        {
            if (channelsToRead != null)
            {
                _ad71248.SetChannel(0, AdcChannel0);
                _ad71248.SetChannel(1, AdcChannel1);
            }

            _ad71248.SetConfiguration(0, AdcConfiguration0);
            _ad71248.SetConfiguration(1, AdcConfiguration1);
        }

        ct.ThrowIfCancellationRequested();
        Dictionary<byte, (double voltage, uint data, byte gain)> results = new();

        foreach (var channel in channelsData)
        {
            var list = channel.Value;
            //Console.WriteLine(string.Join(", ", list.Select(v => $"{v.voltage:0.00000}")));
            list.Sort();
            double median = GetMedianInSortedArray(list, e => e.voltage);
            double averageDeviation = list.Select(v => Math.Abs(median - v.voltage)).Average();
            var correctData = list.Where(v => Math.Abs(median - v.voltage) <= averageDeviation * 3).ToList();
            var averageVoltage = correctData.Average(v => v.voltage);
            uint averageData = (uint)correctData.Average(v => v.data);
            results[channel.Key] = (averageVoltage, averageData, gains[channel.Key].Gain());
            Log($"{channel.Key} > " + string.Join(" | ", channel.Value.Select(v => $"{v.voltage:0.00000}")) +
                " = " + results[channel.Key].voltage.ToString("0.00000"));
        }

        return results;
    }
}