#!/usr/bin/env python3
"""
Script to examine Excel file using excel-mcp-server
This demonstrates how to use MCP tools to read Excel file contents
"""

import asyncio
import json
import sys
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client

async def examine_excel_file():
    """Examine the test.xlsx file using MCP excel server"""
    
    # Server parameters for excel-mcp-server
    server_params = StdioServerParameters(
        command="python",
        args=["-m", "excel_mcp", "stdio"],
        env=None
    )
    
    print("Starting Excel MCP Server examination...")
    print("=" * 60)
    
    try:
        async with stdio_client(server_params) as (read, write):
            async with ClientSession(read, write) as session:
                # Initialize the session
                await session.initialize()
                
                print("MCP Session initialized successfully")
                
                # List available tools
                list_tools_result = await session.list_tools()
                print(f"\nAvailable MCP Tools ({len(list_tools_result.tools)}):")
                for tool in list_tools_result.tools:
                    print(f"  - {tool.name}: {tool.description}")
                
                print("\n" + "=" * 60)
                print("EXAMINING test.xlsx")
                print("=" * 60)
                
                # 1. Get workbook metadata
                print("\n1. Getting Workbook Metadata...")
                try:
                    metadata_result = await session.call_tool("get_workbook_metadata", {
                        "file_path": "test.xlsx"
                    })
                    
                    if metadata_result.content:
                        metadata = json.loads(metadata_result.content[0].text)
                        print(f"   File: {metadata.get('file_path', 'N/A')}")
                        print(f"   Total Worksheets: {len(metadata.get('worksheets', []))}")
                        
                        worksheets = metadata.get('worksheets', [])
                        for i, ws in enumerate(worksheets, 1):
                            print(f"   Sheet {i}: '{ws['name']}' ({ws['used_range']})")
                    
                except Exception as e:
                    print(f"   Error getting metadata: {e}")
                
                # 2. Read data from first worksheet
                print("\n2. Reading Data from 'Sample Data' Worksheet...")
                try:
                    # First, let's read a reasonable range to see what's there
                    read_result = await session.call_tool("read_data_from_excel", {
                        "file_path": "test.xlsx",
                        "worksheet_name": "Sample Data",
                        "range": "A1:E10"
                    })
                    
                    if read_result.content:
                        data = json.loads(read_result.content[0].text)
                        print(f"   Range: A1:E10")
                        print(f"   Rows returned: {len(data.get('data', []))}")
                        
                        # Display the data in a formatted table
                        rows = data.get('data', [])
                        if rows:
                            print("\n   Data Preview:")
                            for i, row in enumerate(rows[:10]):  # Show first 10 rows
                                row_str = " | ".join([str(cell) if cell is not None else "" for cell in row])
                                prefix = "   HEADER: " if i == 0 else "   DATA:   "
                                print(f"{prefix}{row_str}")
                    
                except Exception as e:
                    print(f"   Error reading Sample Data: {e}")
                
                # 3. Read data from second worksheet
                print("\n3. Reading Data from 'Department Summary' Worksheet...")
                try:
                    read_result = await session.call_tool("read_data_from_excel", {
                        "file_path": "test.xlsx",
                        "worksheet_name": "Department Summary",
                        "range": "A1:C10"
                    })
                    
                    if read_result.content:
                        data = json.loads(read_result.content[0].text)
                        print(f"   Range: A1:C10")
                        print(f"   Rows returned: {len(data.get('data', []))}")
                        
                        # Display the data
                        rows = data.get('data', [])
                        if rows:
                            print("\n   Summary Data:")
                            for i, row in enumerate(rows):
                                if row and any(cell is not None for cell in row):
                                    row_str = " | ".join([str(cell) if cell is not None else "" for cell in row])
                                    prefix = "   HEADER: " if i == 0 else "   SUMMARY:"
                                    print(f"{prefix}{row_str}")
                    
                except Exception as e:
                    print(f"   Error reading Department Summary: {e}")
                
                # 4. Get specific cell values
                print("\n4. Reading Specific Cells...")
                try:
                    # Read individual cells
                    cell_ranges = ["A1", "B2", "E2", "A1:A5"]
                    for cell_range in cell_ranges:
                        read_result = await session.call_tool("read_data_from_excel", {
                            "file_path": "test.xlsx",
                            "worksheet_name": "Sample Data",
                            "range": cell_range
                        })
                        
                        if read_result.content:
                            data = json.loads(read_result.content[0].text)
                            rows = data.get('data', [])
                            if rows:
                                if len(rows) == 1 and len(rows[0]) == 1:
                                    # Single cell
                                    value = rows[0][0]
                                    print(f"   {cell_range}: {value}")
                                else:
                                    # Range of cells
                                    values = [str(row[0]) for row in rows if row and row[0] is not None]
                                    print(f"   {cell_range}: [{', '.join(values)}]")
                
                except Exception as e:
                    print(f"   Error reading specific cells: {e}")
                
                print("\n" + "=" * 60)
                print("Excel file examination completed!")
                print("=" * 60)
                
    except Exception as e:
        print(f"Error during examination: {e}")
        print(f"Error type: {type(e).__name__}")
        import traceback
        traceback.print_exc()

if __name__ == "__main__":
    asyncio.run(examine_excel_file())