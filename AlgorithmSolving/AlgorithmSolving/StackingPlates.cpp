
#define _CRT_SECURE_NO_WARNINGS

#include <iostream>
#include <cstdio>
#include <iomanip>
#include <cmath>
#include <vector>
#include <algorithm>
#include <functional>
#include <string>
#include <cstring>
#include <sstream>
#include <fstream>
#include <cstdlib>
#include <stack>
#include <queue>
#include <map>
#include <set>
#include <ctime>
#include <cassert>

using namespace std;

#define REP(v, hi) for (int v=0;v<(hi);v++)
#define REPD(v, hi) for (int v=((hi)-1);v>=0;v--)
#define FOR(v, lo, hi) for (int v=(lo);v<(hi);v++)
#define FORD(v, lo, hi) for (int v=((hi)-1);v>=(lo);v--)
#define REP1(v, hi) for (int v=1;v<=(hi);v++)
#define REPD1(v, hi) for (int v=(hi);v>=1;v--)
#define FOR1(v, lo, hi) for (int v=(lo);v<=(hi);v++)
#define FORD1(v, lo, hi) for (int v=(hi);v>=(lo);v--)
const double eps = 1 / (double)1000000000;
//const int INF = 1 << 30;
const int INF = 9999;

int main(){
	//freopen("StackingPlates.in", "r", stdin);
	freopen("input.txt", "r", stdin);
	freopen("output.txt", "w+", stdout);
	int stackCount;
	int caseNumber = 0;
	while (cin >> stackCount)
	{
		vector<set<int> > stack(stackCount);
		int list[60 * 60] = { 0 };
		int cntList = 0;
		REP(i, stackCount)
		{
			int H, tmp;
			cin >> H;
			REP(j, H)
			{
				cin >> tmp;
				if (stack[i].find(tmp) == stack[i].end())
				{
					stack[i].insert(tmp);
					list[cntList++] = tmp;
				}
			}
		}
		sort(list, list + cntList);

		int pointer[60] = { 0 };
		int dp[60 * 60][60] = { 0 };
		REP(i, 60 * 60) REP(j, 60) dp[i][j] = INF;

		// dp[0] �� ���� ó�� (�׳� ��������.. ��������)
		REP(stackIndex, stackCount)
		{
			dp[0][stackIndex] = (stack[stackIndex].find(list[0]) == stack[stackIndex].end()) ? INF : 0;
		}

		int checkMinNumber = -1;
		int checkMinIndex = -1;


		FOR(listIndex, 1, cntList)
		{
			int currNumber = list[listIndex];
			int prevNumber = list[listIndex - 1];
			REP(stackIndex, stackCount)
			{
				// currNumber �� ���� stack �� �ִ� ��� (�ڡڡڡڡڡ�)
				set<int>& currStack = stack[stackIndex];
				int& currDP = dp[listIndex][stackIndex];
				int& prevDP = dp[listIndex - 1][stackIndex];
				if (currStack.find(currNumber) != currStack.end())
				{
					if (currNumber != prevNumber && dp[listIndex - 1][stackIndex] != INF)
					{
						currDP = prevDP;
					}
					else
					{
						int add = (currNumber == *currStack.begin()) ? 1 : 2;
						REP(j, stackCount) if (j != stackIndex) currDP = min(currDP, dp[listIndex - 1][j] + add);
						if (currNumber == checkMinNumber && stackIndex == checkMinIndex && listIndex >= 2 && currNumber == list[listIndex - 2]) {
							currDP = INF;
						}
					}
				}
			}

			if (currNumber != checkMinNumber)
			{
				// �ּҰ��� �ϳ��� ��쿡 ���� ó��
				// �ּҰ��� ã�´�.
				int minIndex = 0;
				REP(i, stackCount) minIndex = (dp[listIndex][minIndex] > dp[listIndex][i]) ? i : minIndex;

				// �ּҰ��� ����� üũ�Ѵ�.
				int cntMinn = 0;
				REP(i, stackCount) cntMinn += (dp[listIndex][i] == dp[listIndex][minIndex]) ? 1 : 0;

				// �ּҰ��� �Ѱ��̸� ��ġ������. �߰����� ��ġ�� �ʿ�
				if (cntMinn == 1)
				{
					checkMinNumber = currNumber;
					checkMinIndex = minIndex;
				}
				else
				{
					checkMinNumber = -1;
					checkMinIndex = -1;
				}
			}

		}

		REP(i, cntList)
		{
			REP(j, stackCount)
			{
				//cout << dp[i][j] << '\t';
			}
			cout << list[i] << endl;
		}
		int result = INF;
		REP(i, stackCount) result = min(result, dp[cntList - 1][i]);
		//cout << "Case " << ++caseNumber << ": " << result << endl;
		//cout << result << endl;
	}
	return 0;
}
