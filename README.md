# Image Processing

Image Processing is a raster graphics program in node based workflow.

It is written in C# using .Net Framework and Windows Forms.

## Usage examples

### Improving photo quality
![Improve photo nodes](presentation/improve.jpg)
Original|Result
![Improve photo original image](presentation/improveSrc.jpg)|![Improve photo result image](presentation/improveRes.jpg)

### Combining images
![Combining nodes](presentation/combine.jpg)
Result
![Combining](presentation/combineRes.jpg)

### Substituting color
![Substituting color nodes](presentation/subCol.jpg)
Original|Result
![Substituting color original image](presentation/subColSrc.jpg)|![Substituting color result image](presentation/subColRes.jpg)

## Coding interesting points
- To increase performance, optimization using SIMD instructions and multitasking is applied
- Python script can be written to fulfill unusual requests (it is implemented using IronPython library)