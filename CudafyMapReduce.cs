using Cudafy;
using Cudafy.Host;
using Cudafy.Translator;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Project1
{
    [CudafyDummy]
    public struct InputStruct
    {
        public int Value;
    }
    [CudafyDummy]
    public struct CudafyDummyStringKeyIntValue
    {
        public int Key;
        public int Value;
    }
    [CudafyDummy]
    public struct GenericCudafyDummy<T> where T : struct
    {
        public T data;
    }

    public class CudafyMapReduce
    {
        private static ConcurrentDictionary<string, int> freqeuncyDictionary;
        private static readonly char[] separators = { ' ' };
        [Cudafy]
        public struct CudafyStringKeyIntValue
        {
            public int Key;
            public int Value;
            //}
            //[Cudafy]
            //public struct Generic<T> where T: struct
            //{
            //    public T data;
        }
        [Cudafy]
        public static CudafyStringKeyIntValue _map(GThread thread, int input)
        {
            var tid = thread.threadIdx.x;
            var output = new CudafyStringKeyIntValue();
            output.Key = input;
            output.Value = 1;
            return output;
        }

        [Cudafy]
        public static CudafyStringKeyIntValue[] Map(GThread thread, InputStruct[] input)
        {
            var tid = thread.threadIdx.x;
            var output = new CudafyStringKeyIntValue[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                var res = _map(thread, input[i].Value);
                var key = res.Key;
                var value = res.Value;
                output[i].Key = key;
                output[i].Value = value;
            }
            return output;
        }
        [Cudafy]
        public static CudafyStringKeyIntValue[] Reduce(GThread thread, CudafyStringKeyIntValue[] intermediateValues)
        {
            var tid = thread.threadIdx.x;
            int res = 0;
            for (int i = 0; i < intermediateValues.Length - 1; i++)
                for (int j = i + 1; j < intermediateValues.Length; j++)
                {
                    res = StrCmp(thread, intermediateValues[i].Key, intermediateValues[j].Key);
                    if (res > 0)
                    {
                        var tmp = intermediateValues[i];
                        intermediateValues[i] = intermediateValues[j];
                        intermediateValues[j] = tmp;
                    }
                }

            //for (int i = 0; i < intermediateValues.Length - 1; i++)
            //    for (int j = i + 1; j < intermediateValues.Length; j++)
            //    {
            //        res = StrCmp(thread, intermediateValues[i].Key, intermediateValues[j].Key);
            //        if (res == 0)
            //        {
            //            countEq++;
            //        }
            //    }

            //int arr_i = 0;
            //int arr_j = 0;
            //arr[arr_j] = intermediateValues[arr_j];
            //for (int j = arr_i + 1; j < intermediateValues.Length; j++)
            //{
            //    if (intermediateValues[arr_i].Key == intermediateValues[j].Key)
            //    {
            //        arr[arr_i].Value++;
            //        arr_i++;
            //    }
            //    else
            //    {
            //        arr_j++;
            //        arr[arr_j] = intermediateValues[arr_i];
            //        arr_i++;
            //    }
            //}
            var arr = GetUniqueArray(thread, intermediateValues);

            return arr;
        }

        [Cudafy]
        public static void Execute(GThread thread, InputStruct[] input, CudafyStringKeyIntValue[] output)
        {
            var tid = thread.threadIdx.x;
            var res = Reduce(thread, Map(thread, input));
            for (int i=0; i<res.Length; i++)
            {
                output[i].Key = res[i].Key;
                output[i].Value = res[i].Value;
            }
        }

        public ConcurrentDictionary<string, int> Run(List<string> lines)
        {
            GPGPU gpu = CudafyHost.GetDevice(eGPUType.Emulator);
            CudafyTranslator.Language = eLanguage.Cuda;
            eArchitecture arch = gpu.GetArchitecture();         
            CudafyModule km = CudafyTranslator.Cudafy(arch);
            if (km == null || !km.TryVerifyChecksums())
                km = CudafyTranslator.Cudafy(ePlatform.Auto, eArchitecture.sm_20, typeof(InputStruct), typeof(CudafyStringKeyIntValue), typeof(CudafyMapReduce));
            gpu.LoadModule(km);

            freqeuncyDictionary = new ConcurrentDictionary<string, int>();

            Parallel.ForEach(lines, line =>
            {
                var words = line.Split(separators, StringSplitOptions.RemoveEmptyEntries);

                var input = new InputStruct[words.Length];
                CudafyStringKeyIntValue[] output = new CudafyStringKeyIntValue[words.Length];

                for (int i = 0; i < output.Length; i++)
                {
                    output[i].Value = 0;
                }

                Dictionary<int, string> HashDict = new Dictionary<int, string>();

                for (int i = 0; i < input.Length; i++)
                {
                    if (HashDict.ContainsKey(words[i].GetHashCode()))
                        continue;
                    HashDict.Add(words[i].GetHashCode(), words[i]);
                    input[i].Value = words[i].GetHashCode();
                }

                InputStruct[] dev_array1 = gpu.Allocate<InputStruct>(input);
                var devoutput = gpu.Allocate<CudafyStringKeyIntValue>(output);

                gpu.CopyToDevice(input, dev_array1);
                gpu.Launch(1, 1, "Execute", dev_array1, devoutput);
                gpu.CopyFromDevice(devoutput, output);

                for (int i = 0; i < output.Length; i++)
                {
                    if (output[i].Value == 0 || output[i].Key == 0) continue;
                    freqeuncyDictionary.AddOrUpdate(HashDict[output[i].Key], output[i].Value, (key, oldValue) => oldValue + output[i].Value);
                }
            });
            return freqeuncyDictionary;
        }

        [Cudafy]
        public static CudafyStringKeyIntValue[] GetUniqueArray(GThread thread, CudafyStringKeyIntValue[] arr) {
            var tid = thread.threadIdx.x;
            int count = 0;
            bool[] bool_arr = new bool[arr.Length];
            for(int i=0; i<bool_arr.Length; i++)
            {
                bool_arr[i] = false;
            }
            for (int i = 0; i < arr.Length; i++)
            {
                bool isDuplicate = false;
                for (int j = 0; j < i; j++)
                {
                    if (arr[i].Key == arr[j].Key)
                    {
                        isDuplicate = true;
                        break;
                    }
                }

                if (!isDuplicate)
                {
                    bool_arr[i] = true;
                    count++;
                }
            }
            CudafyStringKeyIntValue[] res = new CudafyStringKeyIntValue[count];
            int index = 0;
            for (int i=0;i < bool_arr.Length; i++)
            {
                if (bool_arr[i] == true)
                {
                    res[index] = arr[i];
                    index++;
                }
            }
            return res;
        }

        [Cudafy]
        public static int StrCmp(GThread thread, int str1, int str2)
        {
            var tid = thread.threadIdx.x;
            if (str1 == str2) return 0;
            else
            {
                if (str1 > str2) return 1;
            }
            return -1;
        }
    }
}