using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DV.Logic.Job;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Maps a taken <see cref="Job"/> to GO/HOLD/RED vs the player's consist (**6.13**).
    /// Reuses caller lists so LateUpdate does not allocate when the job id holds.
    /// </summary>
    internal static class JobConsistProbe
    {
        private const BindingFlags InstanceAll =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        internal static JobConsistStatus Evaluate(
            Job job,
            TrainCar? seed,
            List<Car> expectedLogic,
            List<int> expectedIds)
        {
            if (expectedLogic.Count == 0)
            {
                CollectLogicCars(job.tasks, expectedLogic, depth: 0);
            }

            var expected = expectedLogic.Count;
            expectedIds.Clear();
            for (var i = 0; i < expectedLogic.Count; i++)
            {
                var logic = expectedLogic[i];
                if (logic == null)
                {
                    continue;
                }

                TrainCar? train = null;
                try
                {
                    train = LogicCarExtensions.TrainCar(logic);
                }
                catch
                {
                    // skip unresolved
                }

                if (train == null)
                {
                    continue;
                }

                try
                {
                    expectedIds.Add(train.GetInstanceID());
                }
                catch
                {
                    // skip
                }
            }

            var attached = 0;
            var foreign = 0;
            if (seed != null)
            {
                CountConsist(seed, expectedIds, ref attached, ref foreign);
            }

            return JobConsistStatusEval.Evaluate(expected, attached, foreign);
        }

        internal static void FillTaskTrainCars(List<Car> expectedLogic, List<TrainCar> trains)
        {
            trains.Clear();
            if (expectedLogic == null)
            {
                return;
            }

            for (var i = 0; i < expectedLogic.Count; i++)
            {
                var logic = expectedLogic[i];
                if (logic == null)
                {
                    continue;
                }

                TrainCar? train = null;
                try
                {
                    train = LogicCarExtensions.TrainCar(logic);
                }
                catch
                {
                    // skip unresolved
                }

                if (train != null)
                {
                    trains.Add(train);
                }
            }
        }

        internal static void FillAttachedIds(
            TrainCar? seed,
            List<int> expectedIds,
            List<int> attachedIds,
            out int foreign)
        {
            attachedIds.Clear();
            foreign = 0;
            var attached = 0;
            if (seed != null)
            {
                CountConsist(seed, expectedIds, attachedIds, ref attached, ref foreign);
            }
        }

        private static void CountConsist(
            TrainCar seed,
            List<int> expectedIds,
            ref int attached,
            ref int foreign) =>
            CountConsist(seed, expectedIds, attachedIds: null, ref attached, ref foreign);

        private static void CountConsist(
            TrainCar seed,
            List<int> expectedIds,
            List<int>? attachedIds,
            ref int attached,
            ref int foreign)
        {
            IList<TrainCar>? cars = null;
            try
            {
                cars = seed.trainset != null ? seed.trainset.cars : null;
            }
            catch
            {
                cars = null;
            }

            if (cars == null || cars.Count == 0)
            {
                CountFreightCar(seed, expectedIds, attachedIds, ref attached, ref foreign);
                return;
            }

            for (var i = 0; i < cars.Count; i++)
            {
                CountFreightCar(cars[i], expectedIds, attachedIds, ref attached, ref foreign);
            }
        }

        private static void CountFreightCar(
            TrainCar? car,
            List<int> expectedIds,
            List<int>? attachedIds,
            ref int attached,
            ref int foreign)
        {
            if (car == null)
            {
                return;
            }

            try
            {
                if (car.IsLoco)
                {
                    return;
                }
            }
            catch
            {
                return;
            }

            int id;
            try
            {
                id = car.GetInstanceID();
            }
            catch
            {
                return;
            }

            if (ContainsId(expectedIds, id))
            {
                attached++;
                attachedIds?.Add(id);
            }
            else
            {
                foreign++;
            }
        }

        private static bool ContainsId(List<int> ids, int id)
        {
            for (var i = 0; i < ids.Count; i++)
            {
                if (ids[i] == id)
                {
                    return true;
                }
            }

            return false;
        }

        private static void CollectLogicCars(object? tasksObj, List<Car> sink, int depth)
        {
            if (tasksObj == null || depth > 12)
            {
                return;
            }

            if (tasksObj is TransportTask transport)
            {
                try
                {
                    var carsObj = transport.GetType().GetField("cars", InstanceAll)?.GetValue(transport);
                    if (carsObj is IEnumerable enumerable)
                    {
                        foreach (var item in enumerable)
                        {
                            if (item is Car car && !sink.Contains(car))
                            {
                                sink.Add(car);
                            }
                        }
                    }
                }
                catch
                {
                    // ignore
                }

                return;
            }

            if (tasksObj is SequentialTasks sequential)
            {
                CollectLogicCars(GetMember(sequential, "tasks"), sink, depth + 1);
                return;
            }

            if (tasksObj is ParallelTasks parallel)
            {
                CollectLogicCars(GetMember(parallel, "tasks"), sink, depth + 1);
                return;
            }

            if (tasksObj is Task)
            {
                CollectLogicCars(GetMember(tasksObj, "tasks"), sink, depth + 1);
                CollectLogicCars(GetMember(tasksObj, "cars"), sink, depth + 1);
                return;
            }

            if (tasksObj is IEnumerable enumerable2 && tasksObj is not string)
            {
                foreach (var item in enumerable2)
                {
                    CollectLogicCars(item, sink, depth + 1);
                }
            }
        }

        private static object? GetMember(object obj, string name)
        {
            try
            {
                var type = obj.GetType();
                return type.GetField(name, InstanceAll)?.GetValue(obj)
                    ?? type.GetProperty(name, InstanceAll)?.GetValue(obj);
            }
            catch
            {
                return null;
            }
        }
    }
}
