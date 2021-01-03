#!/bin/bash

# Clear theme
rm -rf ./blog

# Clone theme
git clone https://github.com/devrel-kr/gridsome-starter-liebling.git blog

# Copy files to overwrite
rm -rf ./blog/src/assets/images
cp -R ./overwrites/. ./blog

# Copy contents
rm -rf ./blog/content
cp -R ./content/. ./blog/content

# Install npm packages
cd blog
npm install

# Run Gridsome locally
gridsome develop
