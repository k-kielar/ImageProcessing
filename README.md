# Image Processing

Image Processing is a raster graphics program in node based workflow.

It is written in C# using .Net Framework and Windows Forms.

## Usage examples

### Improving photo quality
![Improve photo nodes](presentation/improve.jpg)
#### Original
![Improve photo original image](presentation/improveSrc.jpg)
#### Result
![Improve photo result image](presentation/improveRes.jpg)

### Combining images
![Combining nodes](presentation/combine.jpg)
![Combining](presentation/combineRes.jpg)

### Substituting color
![Substituting color nodes](presentation/subCol.jpg)
#### Original
![Substituting color original image](presentation/subColSrc.jpg)
#### Result
![Substituting color result image](presentation/subColRes.jpg)

## Coding interesting points
- To increase performance, optimization using SIMD instructions and multitasking is applied
- Python script can be written to fulfill unusual requests (it is implemented using [IronPython][1] library)

[1]: https://ironpython.net/