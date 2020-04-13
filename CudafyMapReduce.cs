using Cudafy;
using Cudafy.Host;
using Cudafy.Translator;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace MapReduceCudafy
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
        private static Dictionary<string, int> freqeuncyDictionary = new Dictionary<string, int>();
        private static readonly char[] separators = { ' ' };
        [Cudafy]
        public struct CudafyStringKeyIntValue
        {
            public int Key;
            public int Value;
        }
        //[Cudafy]
        //public static CudafyStringKeyIntValue _map(GThread thread, int input)
        //{
        //    var tid = thread.threadIdx.x;
        //    var output = new CudafyStringKeyIntValue();
        //    output.Key = input;
        //    output.Value = 1;
        //    return output;
        //}

        [Cudafy]
        public static CudafyStringKeyIntValue[] Map(GThread thread, InputStruct[] input)
        {
            var tid = thread.threadIdx.x;
            var output = new CudafyStringKeyIntValue[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i].Value == 0)
                    continue;
                output[i].Key = input[i].Value;
                output[i].Value = 1;
            }
            return output;
        }
        [Cudafy]
        public static CudafyStringKeyIntValue[] Reduce(GThread thread, CudafyStringKeyIntValue[] intermediateValues)
        {
            var tid = thread.threadIdx.x;

            timSort(intermediateValues, intermediateValues.Length);

            var arr = new CudafyStringKeyIntValue[intermediateValues.Length];
            
            findFrequency(thread, intermediateValues, intermediateValues.Length, arr);

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

        public Dictionary<string, int> Run(List<string> lines)
        {

            GPGPU gpu = CudafyHost.GetDevice(eGPUType.Cuda);
            CudafyTranslator.Language = eLanguage.Cuda;
            eArchitecture arch = gpu.GetArchitecture();
            CudafyModule km = CudafyTranslator.Cudafy(arch);
            if (km == null || !km.TryVerifyChecksums())
                km = CudafyTranslator.Cudafy(ePlatform.Auto, eArchitecture.sm_20, typeof(InputStruct), typeof(CudafyStringKeyIntValue), typeof(CudafyMapReduce));
            gpu.LoadModule(km);

            //freqeuncyDictionary = new ConcurrentDictionary<string, int>();

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
                    {
                        input[i].Value = words[i].GetHashCode();
                        continue;
                    }
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
                    if (freqeuncyDictionary.ContainsKey(HashDict[output[i].Key]))
                    {
                        freqeuncyDictionary[HashDict[output[i].Key]] = freqeuncyDictionary[HashDict[output[i].Key]] + output[i].Value;
                        continue;
                    }
                    freqeuncyDictionary.Add(HashDict[output[i].Key], output[i].Value);
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

        //This code is contributed by DrRoot_ 
        [Cudafy]
        public const int RUN = 32;

        // this function sorts array from left index to  
        // to right index which is of size atmost RUN  
        [Cudafy]
        public static void insertionSort(CudafyStringKeyIntValue[] arr, int left, int right)
        {
            for (int i = left + 1; i <= right; i++)
            {
                int temp = arr[i].Key;
                int j = i - 1;
                while (j >= left && arr[j].Key > temp)
                {
                    arr[j + 1].Key = arr[j].Key;
                    j--;
                }
                arr[j + 1].Key = temp;
            }
        }

        // merge function merges the sorted runs  
        [Cudafy]
        public static void merge(CudafyStringKeyIntValue[] arr, int l, int m, int r)
        {
            // original array is broken in two parts  
            // left and right array  
            int len1 = m - l + 1, len2 = r - m;
            int[] left = new int[len1];
            int[] right = new int[len2];
            for (int x = 0; x < len1; x++)
                left[x] = arr[l + x].Key;
            for (int x = 0; x < len2; x++)
                right[x] = arr[m + 1 + x].Key;

            int i = 0;
            int j = 0;
            int k = l;

            // after comparing, we merge those two array  
            // in larger sub array  
            while (i < len1 && j < len2)
            {
                if (left[i] <= right[j])
                {
                    arr[k].Key = left[i];
                    i++;
                }
                else
                {
                    arr[k].Key = right[j];
                    j++;
                }
                k++;
            }

            // copy remaining elements of left, if any  
            while (i < len1)
            {
                arr[k].Key = left[i];
                k++;
                i++;
            }

            // copy remaining element of right, if any  
            while (j < len2)
            {
                arr[k].Key = right[j];
                k++;
                j++;
            }
        }

        // iterative Timsort function to sort the  
        // array[0...n-1] (similar to merge sort)
        [Cudafy]
        public static void timSort(CudafyStringKeyIntValue[] arr, int n)
        {
            // Sort individual subarrays of size RUN  
            for (int i = 0; i < n; i += RUN)
                insertionSort(arr, i, Math.Min((i + 31), (n - 1)));

            // start merging from size RUN (or 32). It will merge  
            // to form size 64, then 128, 256 and so on ....  
            for (int size = RUN; size < n; size = 2 * size)
            {
                // pick starting point of left sub array. We  
                // are going to merge arr[left..left+size-1]  
                // and arr[left+size, left+2*size-1]  
                // After every merge, we increase left by 2*size  
                for (int left = 0; left < n; left += 2 * size)
                {
                    // find ending point of left sub array  
                    // mid+1 is starting point of right sub array  
                    int mid = left + size - 1;
                    int right = Math.Min((left + 2 * size - 1), (n - 1));

                    // merge sub array arr[left.....mid] &  
                    // arr[mid+1....right]  
                    merge(arr, left, mid, right);
                }
            }
        }
        [Cudafy]
        private static void findFrequency(GThread thread, CudafyStringKeyIntValue[] A, int n, CudafyStringKeyIntValue[] map)
        {
            var x = thread.threadIdx.x;
            // search space is A[low..high]
            int low = 0, high = n - 1;

            // run till search space is exhausted
            while (low <= high)
            {
                // A[low..high] consists of only one element,
                // then update its count
                if (A[low].Key == A[high].Key)
                {
                    map[low].Key = A[low].Key;
                    map[low].Value += high - low + 1;

                    // Now discard A[low..high] and continue searching
                    // in A[high+1.. n-1] for A[low]
                    low = high + 1;
                    high = n - 1;
                }
                else
                    // reduce the search space
                    high = (low + high) / 2;
            }
        }
    }
}