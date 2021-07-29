/*
  Filename   : HeapSort.cpp
  Author     : Jingnan Xie, Colin Schwartz
  Course     : CSCI 362
  Description: Implement Heap Sort to sort an int vector
               min-heap is used to sort the vector in non-ascending order
*/

#include <iostream>
#include <vector>


//heapify a node i
//last is the last index of the heap
void heapifyNode(std::vector<int>& sort, int i, int last){
  if(i*2 <= last && i*2+1 <= last){
    if(sort[i*2] < sort[i*2+1] && sort[i] > sort[i*2]){
      std::swap(sort[i],sort[i*2]);
      heapifyNode(sort, i*2, last);
    }
    else if(sort[i*2] > sort[i*2+1] && sort[i] > sort[i*2+1]){
      std::swap(sort[i], sort[i*2+1]);
      heapifyNode(sort, i*2+1, last);
    }
  }
  else if(i*2 <= last){
    if(sort[i] > sort[i*2]){
      std::swap(sort[i], sort[i*2]);
      heapifyNode(sort, i*2, last);
    }
  }

}


//Build a heap
//hint: heapify each internal node from bottom up
void heapify(std::vector<int>& sort, int last){
  for(int i = last; i > 0; i--){
    if(i*2 <= last){
      heapifyNode(sort, i, last);
    }
  }
}

//A heapSort helper to repeat step2-3 of the algorithm
//step2: heapify the root, then
//step3: swap the root with the last
//Hint: Use recursion

void heapSort(std::vector<int>& sort, int last){
  if(last > 1){
    heapifyNode(sort, 1, last);
    std::swap(sort[1], sort[last]);
    heapSort(sort, last - 1);
  }
}

//heapSort to sort an int vector 
void heapSort(std::vector<int>& sort){
  std::vector<int> temp(sort.size() +1);
  std::copy(sort.begin(), sort.end(), temp.begin() +1);  //move all elements one to the right, so index starts with 1
  heapify(temp, sort.size());  //build a heap
  heapSort(temp, sort.size()); //repeat 2-4 
  std::copy(temp.begin()+1, temp.end(), sort.begin());  //copy everything back so index starts with 0
}

int 
main ()  
{
 std::vector<int> myVect ={20, -13, 29, 3, 8 , 17, 13, 10, 26, 0, -11, -5, 7, 19};
 
 heapSort(myVect);
 for(int e: myVect){
   std::cout<<e<<" ";
 }
  return 0;
}
