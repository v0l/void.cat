module.exports = {
  webpack: {
    configure: {
      resolve: {
        fallback: {
          crypto: false,
        },
      },
    },
  },
};
