struct CudafyMapReduceCudafyStringKeyIntValue
{
	__device__  CudafyMapReduceCudafyStringKeyIntValue()
	{
	}
	int Key;
	int Value;
};


// Project1.CudafyMapReduce
__device__  CudafyMapReduceCudafyStringKeyIntValue _map(int input);
// Project1.CudafyMapReduce
__device__   CudafyMapReduceCudafyStringKeyIntValue* Map( InputStruct* input, int inputLen0);
// Project1.CudafyMapReduce
__device__   CudafyMapReduceCudafyStringKeyIntValue* Reduce( CudafyMapReduceCudafyStringKeyIntValue* intermediateValues, int intermediateValuesLen0);
// Project1.CudafyMapReduce
extern "C" __global__  void Execute( InputStruct* input, int inputLen0,  CudafyMapReduceCudafyStringKeyIntValue* output, int outputLen0);
// Project1.CudafyMapReduce
__device__   CudafyMapReduceCudafyStringKeyIntValue* GetUniqueArray( CudafyMapReduceCudafyStringKeyIntValue* arr, int arrLen0);
// Project1.CudafyMapReduce
__device__  int StrCmp(int str1, int str2);

// Project1.CudafyMapReduce
__device__  CudafyMapReduceCudafyStringKeyIntValue _map(int input)
{
	int x = threadIdx.x;
	CudafyMapReduceCudafyStringKeyIntValue result = CudafyMapReduceCudafyStringKeyIntValue();
	result.Key = input;
	result.Value = 1;
	return result;
}
// Project1.CudafyMapReduce
__device__   CudafyMapReduceCudafyStringKeyIntValue* Map( InputStruct* input, int inputLen0)
{
	int x = threadIdx.x;
	 CudafyMapReduceCudafyStringKeyIntValue* array = new CudafyMapReduceCudafyStringKeyIntValue[inputLen0];
	for (int i = 0; i < inputLen0; i++)
	{
		CudafyMapReduceCudafyStringKeyIntValue cudafyStringKeyIntValue = _map(input[(i)].Value);
		int key = cudafyStringKeyIntValue.Key;
		int value = cudafyStringKeyIntValue.Value;
		array[(i)].Key = key;
		array[(i)].Value = value;
	}
	return array;
}
// Project1.CudafyMapReduce
__device__   CudafyMapReduceCudafyStringKeyIntValue* Reduce( CudafyMapReduceCudafyStringKeyIntValue* intermediateValues, int intermediateValuesLen0)
{
	int x = threadIdx.x;
	for (int i = 0; i < intermediateValuesLen0 - 1; i++)
	{
		for (int j = i + 1; j < intermediateValuesLen0; j++)
		{
			int num = StrCmp(intermediateValues[(i)].Key, intermediateValues[(j)].Key);
			bool flag = num > 0;
			if (flag)
			{
				CudafyMapReduceCudafyStringKeyIntValue cudafyStringKeyIntValue = intermediateValues[(i)];
				intermediateValues[(i)] = intermediateValues[(j)];
				intermediateValues[(j)] = cudafyStringKeyIntValue;
			}
		}
	}
	return GetUniqueArray(intermediateValues, intermediateValuesLen0);
}
// Project1.CudafyMapReduce
extern "C" __global__  void Execute( InputStruct* input, int inputLen0,  CudafyMapReduceCudafyStringKeyIntValue* output, int outputLen0)
{
	int x = threadIdx.x;
	 CudafyMapReduceCudafyStringKeyIntValue* array = Reduce(Map(input, inputLen0));
	for (int i = 0; i < arrayLen0; i++)
	{
		output[(i)].Key = array[(i)].Key;
		output[(i)].Value = array[(i)].Value;
	}
}
// Project1.CudafyMapReduce
__device__   CudafyMapReduceCudafyStringKeyIntValue* GetUniqueArray( CudafyMapReduceCudafyStringKeyIntValue* arr, int arrLen0)
{
	int x = threadIdx.x;
	int num = 0;
	 bool* array = new bool[arrLen0];
	for (int i = 0; i < arrayLen0; i++)
	{
		array[(i)] = false;
	}
	for (int j = 0; j < arrLen0; j++)
	{
		bool flag = false;
		for (int k = 0; k < j; k++)
		{
			bool flag2 = arr[(j)].Key == arr[(k)].Key;
			if (flag2)
			{
				flag = true;
				break;
			}
		}
		bool flag3 = !flag;
		if (flag3)
		{
			array[(j)] = true;
			num++;
		}
	}
	 CudafyMapReduceCudafyStringKeyIntValue* array2 = new CudafyMapReduceCudafyStringKeyIntValue[num];
	int num2 = 0;
	for (int l = 0; l < arrayLen0; l++)
	{
		bool flag4 = array[(l)];
		if (flag4)
		{
			array2[(num2)] = arr[(l)];
			num2++;
		}
	}
	return array2;
}
// Project1.CudafyMapReduce
__device__  int StrCmp(int str1, int str2)
{
	int x = threadIdx.x;
	bool flag = str1 == str2;
	int result;
	if (flag)
	{
		result = 0;
	}
	else
	{
		bool flag2 = str1 > str2;
		if (flag2)
		{
			result = 1;
		}
		else
		{
			result = -1;
		}
	}
	return result;
}
