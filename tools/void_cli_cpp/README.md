Building on Windows
===

 1. Setup VCPKG & VS2017
 2. Install deps `vcpkg install cryptopp cxxopts curl nlohmann-json`
 3. Build in VS2017
 
 
Building on Linux
===

 1. Install deps `apt install libcrypto++-dev libcurl4-openssl-dev nlohmann-json-dev`
 2. Get cxxopts header `wget -O /usr/include/cxxopts.hpp https://raw.githubusercontent.com/jarro2783/cxxopts/master/include/cxxopts.hpp`
 3. Build `g++ -O3 -o void_upload main.cpp -I/usr/include -L/usr/local/lib -lcurl -lcryptopp`