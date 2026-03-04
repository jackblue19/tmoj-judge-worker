#include <bits/stdc++.h>
#include <random>
#include <time.h>
// #define int long long
#define ll long long
#define fi first
#define se second
#define ii pair<int,int>
using namespace std;
void file(string f){
    if (fopen((f + ".inp").c_str(),"r")){
        freopen((f + ".inp").c_str(),"r",stdin);
        freopen((f + ".out").c_str(),"w",stdout);
    }
}
mt19937 rng(chrono::steady_clock::now().time_since_epoch().count());
ll range(ll l, ll r){
    return l + (1ULL * rng() * 1ULL * rng() + rng() + rng() + 1) % (1ULL *(r - l + 1));
}
const int mod1 = 1e9 + 7;
const int mod2 = 998244353;
const int N = 3e6 + 10;
ll power(ll a, ll b, ll c){
    ll res = 1;
    a = a % c;
    for (; b > 0; b >>= 1 , a = a * a % c){
        if (b & 1) res = res * a % c;
    }
    return res;
}

struct segtree{
    int n;
    vector <int> st;
    segtree(int Nsize){
        n = Nsize;
        st.assign(4 * Nsize, 1e18);
    }
    void update(int k, int l, int r, int pos, int val){
        if (l == r && r == pos){
            st[k] = val;
            return;
        }
        int mid = (l + r) >> 1;
        if (pos <= mid) update(k << 1,l,mid,pos,val);
        else update(k<<1|1,mid+1,r,pos,val);
        st[k] = min(st[k<<1],st[k<<1|1]);
    }
    int query(int k, int l, int r, int u, int v){
        if (l > v || r < u) return 1e18;
        if (l >= u && r <= v) return st[k];
        int mid = (l + r) >> 1;
        return min(query(k<<1,l,mid,u,v), query(k<<1|1,mid+1,r,u,v));
    }
    void update(int pos, int val){
        update(1,1,n,pos,val);
    }
    int query(int u, int v){
        return query(1,1,n,u,v);
    }
};
int a[N], dp[5003], res[N];
int n , q;
int bitmax[5003][5003];
void update(int L, int x, int val){
    for (; x <= n; x += x&(-x)){
        bitmax[L][x] = max(bitmax[L][x], val);
    }
}
int query(int L, int x){
    int res = 0;
    for (; x > 0; x -= x&(-x)){
        res = max(res, bitmax[L][x]);
    }
    return res;
}
void solve(){
    cin >> n >> q;
    for (int i = 1; i <= n; i++) cin >> a[i];
    vector <int> v; for (int i = 1; i <= n; i++) v.push_back(a[i]);
    sort(v.begin(), v.end());
    v.resize(unique(v.begin(), v.end()) - v.begin());
    for (int i = 1; i <= n; i++) a[i] = lower_bound(v.begin(), v.end(),a[i]) - v.begin() + 1;

    // for (int i = 1; i <= n; i++) cout << a[i] << " "; cout << "\n";

    for (int L = 1; L <= n; L++){
        for (int i = 1; i <= n; i++){
            dp[i] = 1;
            if (i - L >= 1) update(L, a[i - L], dp[i - L]);
            dp[i] = max(dp[i], query(L, a[i]) + 1);
            res[L] = max(res[L], dp[i]);
        }
        // for (int i = 1; i <= n; i++) cout << dp[i] << " "; cout << "\n";
    }
    while (q--){
        int L; cin >> L;
        cout << res[L] << "\n";
    }
}
signed main(){
    srand(time(NULL));
    ios::sync_with_stdio(false); cin.tie(NULL);
    // file("icpc_prob_006");
    int t = 1;
    // cin >> t;
    for (int _ = 1; _ <= t; _++){
        solve();
    }
    return 0;
}
